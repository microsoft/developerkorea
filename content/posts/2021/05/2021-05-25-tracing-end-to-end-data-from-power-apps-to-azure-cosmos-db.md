---
title: "파워 앱의 종단간 데이터 흐름 실시간 추적"
slug: tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db
description: "이 포스트에서는 퓨전 개발팀에서 개발한 파워 앱에서 코스모스 DB까지 종단간 데이터 흐름을 오픈 텔레메트리 기법을 이용해 실시간으로 추적하는 방법에 대해 알아봅니다."
date: "2021-05-25"
image: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db-00.png
image_caption: "여러 점들의 연결"
author: justin-yoo
category: Power Platform
tags: azure, observability, traceability, open-telemetry
canonical_url: https://blog.aliencube.org/ko/2021/05/19/tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db/
featured: false
---

클라우드 기반의 애플리케이션 아키텍처를 보고 있노라면, 굳이 마이크로서비스 아키텍처가 아니라고 하더라도 수많은 시스템들이 복잡하게 연결되어 서로 데이터 혹은 메시지를 주고 받는 경우가 대부분입니다. 이런 경우에 서로 다른 시스템으로 메시지가 오가다 보면 어느 순간 메시지를 잃어버린다든가, 처리가 되지 않은 채로 계속 남아있다든가 하는 경우가 종종 생기곤 하지요.

클라우드 환경에서는 시스템을 구성하는 수많은 컴포넌트들이 저마다의 리듬으로 작동하기 때문에 어느 특정한 시점에서 특정 서비스가 다운될 수 있다는 것을 항상 전제 조건으로 하고 아키텍처를 설계해야 합니다. 따라서, 메시지가 처음부터 끝까지 무슨 경로를 통해 어떻게 흘러가는지를 지속적으로 확인할 수 있게끔 설계하는 것이 좋은데요, 이런 기능을 가리켜 관측 용이성(Observability)과 추적 용이성(Traceability)이라고 합니다.

[지난 포스트][post 1]에서는 퓨전 개발팀 안에서 시민 개발자가 OpenAPI 기능을 추가한 [애저 펑션][az fncapp] 기반의 API를 이용해 [파워 앱][pa]을 개발하는 과정에 대해 알아보았습니다. 이번에는 이 파워 앱에서 데이터베이스까지 데이터가 이동하는 과정을 [애저 모니터][az monitor] 서비스와 [애플리케이션 인사이트][az appins] 서비스를 이용해 추적하는 방법에 대해 알아보고, 이를 [오픈 텔레메트리][cncf opentelemetry]에서 제공하는 개념과 연결지어 논의해 보기로 합니다.

* [퓨전 개발팀의 파워 앱 개발 실사례][post 1]
* ***파워 앱의 종단간 데이터 흐름 실시간 추적***
* 파워 앱에 DevOps 적용하기

> 이 포스트에 사용한 백엔드 API 샘플 코드는 이곳 [GitHub 리포지토리][gh sample]에서 다운로드 받을 수 있습니다.


## 시나리오 ##

Lamna 피트니스 센터에서는 회원들을 위해 [파워 앱][pa]으로 만든 모바일 앱으로 운동 일지를 작성하는 서비스를 제공하고 있습니다. 그런데, 종종 데이터 입력시 오류가 난다는 회원들의 피드백을 받곤 하는데요, 퓨전 개발팀에서 퍼스널 트레이너 팀을 대표하는 안지민쌤은 이 문제를 같은 팀의 전문 개발자인 권수빈쌤과 공유했습니다. 따라서, 수빈쌤은 어떻게 데이터가 파워 앱에서 애저 코스모스 DB로 흘러가는지 추적할 수 있는 로직을 추가하기로 방향을 잡고 애플리케이션을 수정하기로 했습니다. 데이터 추적 프로세스를 그림으로 나타내면 아래와 같습니다.

![GymLog Telemetry Architecture][image-01]

위의 프로세스를 [오픈 텔레메트리][cncf opentelemetry]에서 정의한 스펙에 맞춰 분석해 볼까요?

* [파워 앱][pa]에서 [코스모스 DB][az cosdba]까지 데이터가 흘러가는 전체 흐름을 [트레이스(trace)][cncf opentelemetry trace]라고 합니다.
* 이 전체 흐름은 [애저 서비스 버스][az svcbus]를 경계로 삼아 서비스 버스에 메시지를 보내기 전과 후로 나눌 수 있는데, 서비스 버스 전과 후는 완전히 다른 독립적인 애플리케이션(퍼블리셔와 섭스크라이버)이기 때문입니다. 이렇게 나뉘어진 각각을 가리켜 [스팬(span)][cncf opentelemetry span]이라고 합니다. 즉, 스팬은 데이터 혹은 메시지를 처리하는 하나의 작업 단위라고 할 수 있죠.
  * 파워 앱에서 보낸 데이터는 [애저 테이블 저장소][az st table]에 저장되고 마지막 `publish` 액션을 통해 합산(aggregate)된 데이터가 [애저 서비스 버스][az svcbus]로 전송됩니다.

  > 위 그림에서는 이 부분이 `routine`, `exercise`, `publish`의 세 가지 액션으로 구성되어 있습니다. 따라서, 이 부분을 세 개의 스팬으로 잘게 쪼갤 수도 있습니다만, 여기서는 하나의 스팬으로 처리하기로 하겠습니다.

  * [애저 서비스 버스][az svcbus]에서 받은 메시지를 처리해서 [코스모스 DB][az cosdba]에 저장합니다.
* 한 스팬에서 다른 스팬으로 데이터가 이동할 때 데이터 추적에 필요한 메타 데이터가 함께 이동해야 합니다. 이를 [스팬 컨텍스트(span context)][cncf opentelemetry spancontext]라고 합니다.


## 파워 앱 개선 ##

앞서 언급한 바와 같이 트레이스는 파워 앱에서부터 시작하기 때문에, 파워 앱에서 트레이스를 위해 작업해 줘야 할 것이 있습니다. 아래 그림과 같이 **Start** 버튼을 누를 때 `correlationId` 값과 `spanId` 값을 파워 앱에서 준비해서 첫번째 `routine` 액션을 수행할 때 API로 보내는 것이 되겠네요.

![Power Apps Canvas - Correlation ID and Span ID][image-02]

이렇게 함으로써, 파워 앱에서부터 추적을 시작한다는 것을 전체 모니터링 과정에서 알 수 있고, 첫번째 스팬도 파워 앱에서부터 시작한다는 것 역시도 알 수 있습니다. 여기서 생성한 `correlationId` 값과 `spanId` 값은 `publish` 액션을 수행할 때 까지 계속해서 따라다닙니다. 더우기 `correlationId` 값은 애저 서비스 버스를 통해 다른 스팬으로 넘어갈 때에도 스팬 컨텍스트를 통해 계속해서 다른 스팬으로 전달됩니다.


## 백엔드 개선 ##

애저 펑션은 [애플리케이션 인사이트][az appins] 서비스의 계측 키(instrumentation key)만 연결해 놓으면 자동으로 모든 추적 데이터를 수집합니다. [오픈 텔레메트리][cncf opentelemetry]의 구현체 중 하나인 [OpenTelemetry.NET][cncf opentelemetry dotnet]을 보면 현재 추적 관련 구현은 [1.0 버전으로 안정화 되었고][cncf opentelemetry dotnet ga], 메트릭이라든가 세부 로그 작성 관련해서는 곧 GA를 목표로 활발하게 작업중입니다. 하지만 안타깝게도 이 구현체가 [애저 펑션에서는 제대로 작동하지 않습니다][cncf opentelemetry dotnet issue]. 따라서, 여기서는 오픈 텔레메트리의 추적 과정을 로그 저장 수준에서만 구현해서 애플리케이션 인사이트와 통합해 보기로 하겠습니다.


### 퍼블리셔 &ndash; HTTP 트리거 ###

그렇다면 로그는 어느 시점에 저장하는 것이 좋을까요?

백엔드 API는 `routine`, `exercise`, `publish` 액션으로 구성되어 있고, 각 액션은 [애저 테이블 저장소][az st table]에 이벤트 소싱 형태로 데이터를 일시 저장합니다. 따라서, 테이블에 데이터를 저장하는 시점에 추적을 위한 일종의 체크포인트로서 로그를 하나 남기는 것이 좋습니다. 또한, 마지막 `publish` 액션에서는 이렇게 저장된 데이터를 합산(aggregate)해서 [애저 서비스 버스][az svcbus]로 메시지를 보내는데, 메시지를 보낸 후에도 체크포인트 로그를 남겨 놓습니다.

애저 펑션의 로깅 기능은 `ILogger` 인터페이스를 통해 사용할 수 있는데, 애플리케이션 인사이트와 연동시켜 놓으면 사용자 지정 텔레메트리 속성을 손쉽게 저장합니다. 그렇다면 사용자 지정 텔레메트리 속성으로 어떤 것들을 지정하면 좋을까요?

* **이벤트 종류**: 액션과 액션의 수행 여부 &ndash; `RoutineReceived`, `ExerciseCreated`, `MessageNotPublished` 등
* **이벤트 상태**: 성공 혹은 실패 &ndash; `Succeeded`, `Failed` 등
* **이벤트 ID**: 애저 펑션의 Invocation ID로 실행될 때 마다 새롭게 GUID가 할당됨
* **스팬 종류**: 스팬의 종류 &ndash; `Publisher` 또는 `Subscriber`
* **스팬 상태**: 스팬의 상태 &ndash; `PublisherInitiated`, `SubscriberInProgress`, `PublisherCompleted` 등
* **스팬 ID**: 매번 스팬이 실행될 때 마다 새롭게 할당되는 GUID
* **인터페이스 종류**: 사용자 인터페이스의 종류 &ndash; `Test Harness` 또는 `Power Apps App`
* **코릴레이션 ID**: 트레이스 전체에서 메시지 추적을 위한 고유 ID

이 정도를 [애플리케이션 인사이트][az appins]를 통해 저장해 두면, 어떤 트레이스에서(코릴레이션 ID) 무슨 인터페이스를 통해(테스트 혹은 파워 앱), 어떤 스팬을 거쳐서, 데이터 처리(이벤트 종류)에 성공했는지 여부(이벤트 상태)를 한 눈에 알 수 있겠죠?

이를 위해 `ILogger` [확장 메서드를 하나 구현][gh sample logger]해서 애저 펑션 안에서 아래와 같이 사용합니다. 코드에서 보이다시피 코릴레이션 ID 값과 스팬 ID 값은 파워 앱에서 보내온 것을 사용합니다 (line #9-10). 그리고, 이벤트 ID 값은 애저 펑션의 `invocationId` 값을 그대로 사용하면 되겠네요 (line #12). 마지막으로 이벤트 종류, 이벤트 상태, 스팬 종류, 스팬 상태, 인터페이스 종류, 코릴레이션 ID 값을 로그로 보냅니다 (line #14-17). 아래의 코드는 파워 앱으로부터 `routine` 액션을 수행할 때, 요청 데이터를 문제 없이 받았음을 기록하는 부분입니다.

https://gist.github.com/justinyoo/9679433b6d886897cc09d8bcf1c8b6de?file=01-create-routine-01.cs&highlights=9-10,12,14-17

위와 같이 우선 파워 앱으로부터 요청 데이터를 잘 받았음을 체크포인트 로그로 남겨뒀으니, 이제 아래 코드를 볼까요? 우선 요청 데이터를 애저 테이블 저장소에 저장합니다 (line #14). 이 때 성공적으로 저장했다면 거기에 맞춰 로그를 기록합니다 (line #18-23). 만약 이 과정에서 에러가 발생했다면 예외 처리를 하면서 에러 로그를 작성하면 되겠네요 (line #29-34).

https://gist.github.com/justinyoo/9679433b6d886897cc09d8bcf1c8b6de?file=02-create-routine-02.cs&highlights=14,18-23,29-34

이와 비슷한 방식으로 다른 `exercise`, `publish` 액션에도 체크포인트 로그 처리를 합니다.


### 퍼블리셔 &ndash; 스팬 컨텍스트 ###

`publish` 액션에서는 체크포인트 로그 기능을 구현할 뿐만 아니라, 스팬 컨텍스트 처리도 함께 해야 합니다. 스팬 컨텍스트는 스팬을 넘나들면서 데이터 추적과 관련한 메타 데이터를 함께 보내야 하는 부분인데, 이게 HTTP 요청/응답을 사용할 경우에는 요청 헤더에, 애저 서비스 버스를 사용할 경우에는 메시지 봉투에 넣어주는 형태로 구현하면 됩니다. 여기서는 애저 서비스 버스를 사용하므로 메시지 봉투에 구현된 `ApplicationProperties` 딕셔너리를 이용하겠습니다.

아래 `publish` 액션에 대한 애저 펑션 코드를 볼까요? 서비스 버스 메시지의 본문은 운동 기록에 대한 것이지만 (line #23-24), 나머지 데이터는 메시지 개체의 `CorrelationId`, `MessageId` 속성에 (line #26-27), 나머지는 `ApplicationProperties` 딕셔너리를 통해 섭스크라이버 쪽에서 활용할 수 있게끔 지정해 줍니다 (line #30-33). 마지막으로 애저 서비스 버스에 메시지를 보내고 나면 체크포인트 로그를 생성합니다 (line #37-42).

https://gist.github.com/justinyoo/9679433b6d886897cc09d8bcf1c8b6de?file=03-publish-routine.cs&highlights=23-24,26-27,30-33,37-42


### 섭스크라이버 &ndash; 서비스 버스 트리거 ###

위에서 정의한 바와 같이 퍼블리셔 쪽에서 애저 서비스 버스의 메시지 봉투를 이용해서 스팬 컨텍스트를 전달했으니, 섭스크라이버 쪽에서는 이를 받아서 사용합니다.

아래 코드는 메시지 봉투를 해석하는 과정을 표현한 것인데요, 스팬 컨텍스트를 통해 코릴레이션 ID 값을 복원하고 (line #10), 메시지 ID 값을 복원한다 (line #13). 그리고 메시지를 성공적으로 복원한 결과를 체크포인트 로그에 남겨둔다 (line #16-19).

https://gist.github.com/justinyoo/9679433b6d886897cc09d8bcf1c8b6de?file=04-ingest-01.cs&highlights=10,13,16-19

마지막으로 애저 코스모스 DB에 레코드를 저장하고 (line #12), 체크포인트 로그를 남겨둡니다 (line #16-21). 혹시 이 과정에서 에러가 발생할 경우 예외 처리를 한 후 마찬가지로 체크포인트 로그를 저장합니다 (line #25-30).

https://gist.github.com/justinyoo/9679433b6d886897cc09d8bcf1c8b6de?file=05-ingest-02.cs&highlights=12,16-21,25-30

여기까지 해서 데이터가 지나가는 길목마다 체크포인트를 두고, 해당 체크포인트마다 적절한 로그를 애플리케이션 인사이트로 저장하게끔 구현했습니다. 그렇다면, 이제 실제로 이를 [애저 모니터 서비스][az monitor]를 이용해서 어떻게 추적할 수 있는지 살펴 볼까요?


## 애저 모니터링 KUSTO 쿼리 ##

안지민쌤은 다시 회원으로부터 파워 앱에서 운동 일지를 저장하는 과정에서 에러가 생겼다는 피드백을 받았습니다. 마침 이번에는 스크린샷도 함께 받았는데, 대략 아래와 같군요.

![Power Apps Workout Screen][image-03]
![Power Apps Error Screen][image-04]

지민쌤은 이 스크린샷을 수빈쌤과 공유했고, 이를 바탕으로 수빈쌤은 아래와 같은 [Kusto 쿼리][az monitor kusto]를 애플리케이션 인사이트 쿼리 화면에서 실행시켰습니다. 우선 이 에러를 추적하기 위해 `correlationId` 값을 변수로 지정했습니다 (line #1). 그리고 앞서 저장했던 사용자 지정 텔레메트리 속성값을 이용해서 쿼리를 작성했습니다. 모든 사용자 지정 텔레메트리 속성 값은 `customDimensions.prop__`으로 시작하므로 `where` 구문에서 해당 코릴레이션 ID 값으로 필터링을 했고 (line #4), `project` 구문에서는 전체 필드가 아닌 내가 원하는 필드만 지정했습니다 (line #5-18).

https://gist.github.com/justinyoo/9679433b6d886897cc09d8bcf1c8b6de?file=06-kusto-query.kql&highlights=1,4,5-18

그리고 이렇게 해서 쿼리를 실행 시킨 결과는 아래와 같습니다. 운동 메시지를 받는 데 까지는 성공했지만, 이를 처리해서 애저 테이블 저장소에 저장하는 과정에서 에러가 생긴 것을 알 수 있네요.

![Application Insights Kusto Query Result - Failed][image-05]

수빈쌤이 어느 부분에서 오류가 발생했는지 알아냈으니, 이 부분을 수정해서 다시 애저 펑션 API를 배포했고, 이제는 더이상 오류가 발생하지 않았습니다. 아래는 성공한 기록 중 하나를 임의로 조회한 결과입니다. 퍼블리셔 쪽에서 보낸 메시지를 섭스크라이버 쪽에서 제대로 받아 처리하고 애플리케이션 로직에 따라 이 메시지 ID 값이 그대로 코스모드 DB의 레코드 ID 값이 된 것이 보이나요?

![Application Insights Kusto Query Result - Succeeded][image-06]

이렇게 해서 데이터 추적 로직이 [애플리케이션 인사이트][az appins]를 통해 [오픈 텔레메트리][cncf opentelemetry]의 형식을 빌어 구현한 것을 확인했습니다. 이제 지민쌤과 트레이너들, 그리고 모든 회원들이 앱을 사용하다가 에러가 생기면 코릴레이션 ID 값만 불러주면 손쉽게 어디에서 문제가 생겼는지 추적할 수 있겠군요!

---

지금까지 퓨전 개발팀에서 개발한 앱을 두고, 처음부터 끝까지 하나의 ID 값을 이용해 데이터가 이동하는 과정을 추적하는 것을 [오픈 텔레메트리][cncf opentelemetry]와 [애플리케이션 인사이트][az appins]를 이용해 구현했습니다.

아쉽게도 오픈 텔레메트리 닷넷 구현체가 아직 애저 펑션에서 완벽하게 작동하지 않긴 하지만, 여전히 애플리케이션 인사이트를 이용해 오픈 텔레메트리의 사상을 구현해 낼 수 있으므로 당분간 큰 문제는 없을 것으로 생각합니다. 곧 애플리케이션 인사이트와 오픈 텔레메트리간 통합이 애저 펑션에서도 이루어지길 기대합니다.

다음 포스트에서는 파워 앱을 DevOps 기법을 이용해 배포 자동화 하는 방법을 살펴보기로 하겠습니다.


## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][az account free]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt msdevkr]


[az account free]: https://azure.microsoft.com/ko-kr/free/?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813
[ms learn]: https://docs.microsoft.com/ko-kr/learn/?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813
[yt msdevkr]: https://www.youtube.com/c/microsoftdeveloperkorea


[image-01]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db-01.png
[image-02]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db-02.png
[image-03]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db-03.png
[image-04]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db-04.png
[image-05]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db-05.png
[image-06]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db-06.png


[post 1]: /developerkorea/posts/2021/05/18/power-apps-in-fusion-teams/
[post 2]: /developerkorea/posts/2021/05/25/tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db/

[gh sample]: https://github.com/aliencube/GymLog
[gh sample logger]: https://github.com/aliencube/GymLog/blob/main/src/GymLog.FunctionApp/Extensions/LoggerExtensions.cs

[cncf]: https://cncf.io/
[cncf opentelemetry]: https://opentelemetry.io/
[cncf opentelemetry dotnet]: https://opentelemetry.io/docs/net/
[cncf opentelemetry dotnet ga]: https://devblogs.microsoft.com/dotnet/opentelemetry-net-reaches-v1-0/?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813
[cncf opentelemetry dotnet issue]: https://github.com/open-telemetry/opentelemetry-dotnet/issues/1602
[cncf opentelemetry trace]: https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/overview.md#traces
[cncf opentelemetry span]: https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/overview.md#spans
[cncf opentelemetry spancontext]: https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/overview.md#spancontext

[az fncapp]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813

[az svcbus]: https://docs.microsoft.com/ko-kr/azure/service-bus-messaging/service-bus-messaging-overview?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813
[az cosdba]: https://docs.microsoft.com/ko-kr/azure/cosmos-db/introduction?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813

[az st table]: https://docs.microsoft.com/ko-kr/azure/storage/tables/table-storage-overview?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813

[az appins]: https://docs.microsoft.com/ko-kr/azure/azure-monitor/app/app-insights-overview?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813
[az monitor]: https://docs.microsoft.com/ko-kr/azure/azure-monitor/overview?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813
[az monitor kusto]: https://docs.microsoft.com/ko-kr/azure/azure-monitor/logs/log-query-overview?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813

[pa]: https://powerapps.microsoft.com/ko-kr/?WT.mc_id=dotnet-28936-juyoo&ocid=AID3027813
