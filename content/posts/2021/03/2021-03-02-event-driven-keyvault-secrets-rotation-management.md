---
title: "이벤트 기반 애저 키 저장소 시크릿 로테이션 관리"
slug: event-driven-keyvault-secrets-rotation-management
description: "이 포스트에서는 애저 키 저장소의 시크릿 값이 새롭게 갱신될 때 해당 이벤트를 캡쳐해서 로테이션에 필요하지 않은 오래된 버전들을 비활성화 시키는 방법에 대해 알아봅니다."
date: 2021-03-02
image: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-00.png
image_caption: 이벤트 기반 애저 키 저장소 시크릿 관리하기
author: justin-yoo
category: Azure
tags: azure-keyvault, azure-eventgrid, azure-logic-apps, azure-functions, azure-sdk
canonical_url: https://blog.aliencube.org/ko/2021/02/24/event-driven-keyvault-secrets-rotation-management/
featured: false
---


[지난 포스트][post prev 1]에서는 [애저 키 저장소][az kv]의 모든 [시크릿][az kv secrets] 값의 모든 버전을 한꺼번에 돌려보고 비활성화 시키는 방법에 대해 알아 보았습니다. 이 방법이 매우 간편하긴 하지만, 처음 한 번이나 두 번 정도 벌크로 진행할 때를 제외하고는 매번 모든 시크릿 값을 대상으로 진행하는 것은 어찌 보면 과하지 않을까 하는 느낌도 있습니다. 그렇다면, 특정 시크릿에 새 버전이 만들어졌을 때 그 시크릿만 체크해서 필요한 만큼 변경할 수 있는 방법은 없을까요? 물론 있습니다! 애저 키 저장소 인스턴스는 기본적으로 [애저 이벤트그리드][az evtgrd]를 통해 이벤트를 발생시키므로, 이를 활용하면 이벤트 기반으로 특정 시크릿에 대해서만 [버전 로테이션을 관리][az kv secrets rotation]할 수 있습니다.

* [애저 키 저장소 시크릿 로테이션 관리][post prev 1]
* ***이벤트 기반 애저 키 저장소 시크릿 로테이션 관리***

이 포스트에서는 [애저 이벤트그리드][az evtgrd], [애저 로직 앱][az logapp], [애저 펑션][az fncapp]을 이용해서 특정 시크릿에 새 버전이 추가될 경우, 이를 자동으로 감지해서 로테이션 버전을 관리하는 방법에 대해 알아보기로 하겠습니다.

> 실제 작동하는 코드를 보고 싶으신가요? 이 [깃헙 리포지토리][gh sample]에서 다운로드 받아 로컬에서 돌려보세요!


## 애저 키 저장소 이벤트 ##

애저 키 저장소는 [이벤트 그리드][az kv evtgrd]와 통합하여 시크릿에 새로 버전이 하나 추가될 때 마다 [이벤트를 발생시킵니다][az kv evtgrd type]. 따라서 이 이벤트를 받아서 처리하게 되면, 굳이 모든 시크릿을 한꺼번에 루프로 돌리지 않아도 개별 시크릿에 대해서만 작업할 수 있어서 상당히 편리합니다. 애저 키 저장소와 이벤트 그리드, 그리고 이벤트 처리기로서 [애저 로직 앱][az logapp]과 [애저 펑션 앱][az fncapp]을 통합한 전체 아키텍처는 대략 아래와 같습니다.

![전체 E2E 프로세스 아키텍처][image-01]

이벤트 처리기로 [애저 로직 앱][az logapp]을 사용하는 이유는 [이전 포스트][post prev 2]에서 언급한 바와 같이 [이벤트 그리드에서 요구하는 인증][az evtgrd delivery auth]을 내부적으로 손쉽게 처리할 수 있기 때문인데요, 만약 애저 펑션에서 직접 처리하고 싶다면 [이 포스트][post prev 3]를 참조하면 좋습니다.

애저 키 저장소에 이벤트 처리기로서 로직 앱을 연동 시키는 방법은 크게 두 가지가 있습니다. 하나는 애저 키 저장소에서 [이벤트그리드 커넥터][az logapp connectors evtgrd]를 이용해 직접 로직 앱 인스턴스를 생성하는 방법이고, 다른 하나는 별도로 인스턴스를 생성한 후 HTTP 웹훅을 이용해 연동시키는 방법입니다. 전자의 경우 [커넥터][az logapp connectors]라는 의존성이 하나 생기는 반면, 후자는 키 저장소와 로직 앱이 독립적으로 작동하기 때문에 개인적으로는 의존성을 없앤 후자를 선호합니다.

먼저 애저 로직 앱 인스턴스를 하나 만들고 [HTTP 트리거][az logapp connectors request]를 추가합니다.

![로직앱 HTTP 트리거][image-02]

이렇게 만들어 놓은 로직 앱의 URL을 이용해서 키 저장소에서 이벤트 처리기를 연동시켜 보겠습니다. 먼저 키 저장소 인스턴스의 이벤트 블레이드로 이동합니다. 그리고 `+ 이벤트 구독` 버튼을 클릭합니다.

![이벤트 구독 생성 버튼][image-03]

그러면 이벤트 구독 관련 인스턴스를 생성하는 화면이 나오는데, 아래와 같이 `이벤트 구독 인스턴스 이름`, `이벤트 스키마`, `시스템 토픽 인스턴스 이름`, `엔드포인트 정보`, `웹훅 엔드포인트 URL`을 선택합니다.

![이벤트 구독 생성 상세 정보][image-04]

* 이벤트 구독 정보 섹션에서는 이벤트 스키마로 [클라우드 이벤트 스키마 v1.0][ce spec http]을 선택했는데, 이는 애저 자체 표준 대신 [CNCF][cncf]에서 제시하는 표준인 [클라우드이벤트 스펙][ce spec]을 따르는 것이 향후 이기종간 통합에 편리하기 때문입니다.
* 항목 정보 섹션에서는 이벤트 그리드 토픽 이름을 선택합니다.
* 이벤트 유형 섹션에서는 이벤트 형식 중에서 `Secret New Version Created` 이벤트만 선택합니다.
* 엔드포인트 정보 섹션에서는 엔드포인트 유형으로 웹훅/웹후크를 선택하고 엔드포인트 값을 앞서 만들어 놓은 로직 앱 엔드포인트 URL로 설정합니다.

이렇게 하면 애저 키 저장소 인스턴스와 애저 이벤트그리드, 애저 로직 앱 사이에 이벤트 발생시 기본적으로 처리할 수 있는 파이프라인을 만들게 되는데요, 실제로 애저 키 저장소에서 시크릿의 새 버전을 하나 만들어 보면 아래와 같은 이벤트가 발생하는 것을 알 수 있습니다. 원하는 `Microsoft.KeyVault.SecretNewVersionCreated` 형식의 이벤트를 캡쳐한 것이 보입니다.

![로직 앱 이벤트 캡쳐][image-05]

그리고, 실제 이벤트 메시지 JSON 개체는 아래와 같이 생겼습니다.

![로직 앱 이벤트 메시지 페이로드][image-06]

`data` 속성 안에 보면 `ObjectName`이라고 있는데, 바로 이것이 시크릿 이름이고, 이 값을 앞으로 구현할 애저 펑션으로 보내서 해당 시크릿 이름에 대해서만 필요한 버전만 남겨두고 나머지를 비활성화 시키도록 할 예정입니다. 이제 애저 펑션의 구현을 살펴보도록 하겠습니다.


## 애저 펑션을 통해 특정 시크릿에 대해서만 로테이션 관리하기 ##

[지난 포스트][post prev 1]의 구현 내용과 크게 다르지 않지만, 이번에는 특정 시크릿에 대해서만 처리를 하는 내용이므로 펑션의 구현 내용이 살짝 간결해집니다. 먼저 새 [HTTP 트리거][az fncapp trigger http]를 하나 생성합니다.

https://gist.github.com/justinyoo/948385359cc739a48ad5afdf07db932e?file=01-func-new-http-trigger.sh

기본 템플릿을 이용해 HTTP 트리거를 만들었습니다. 이제 이 펑션 메소드의 `HttpTrigger` 바인딩 설정을 아래와 같이 바꿉니다. HTTP 메소드는 `POST` 하나로 한정하고, 라우팅 URL을 `secrets/{name}/disable/{count:int?}` 처럼 바꿉니다 (line #5). 라우팅 URL에 보면 `{name}`, `{count:int?}` 같은 플레이스홀더 두 개가 보이는데, 이는 바로 `string name`, `int? count` 같은 파라미터 변수로 치환시킬 수 있습니다 (line #6).

https://gist.github.com/justinyoo/948385359cc739a48ad5afdf07db932e?file=02-disable-secret-http-trigger-01.cs&highlights=5,6

환경 변수를 통해 아래 두 값을 받아옵니다. 하나는 애저 키 저장소에 접근할 수 있는 URI이고, 다른 하나는 애저 키 저장소 인스턴스를 호스팅하는 테넌트의 ID값입니다.

https://gist.github.com/justinyoo/948385359cc739a48ad5afdf07db932e?file=02-disable-secret-http-trigger-02.cs

다음으로는 애저 키 저장소에 접근할 수 있는 `SecretClient` 인스턴스를 생성한다. 이 때 인증 옵션을 `DefaultAzureCredentialOptions` 인스턴스를 통해 제공해야 하는데, 만약 개발하려는 로컬 컴퓨터에서 애저에 로그인한 계정이 여러 개의 테넌트 정보를 갖고 있다면, 아래와 같이 명시적으로 테넌트 ID 값을 지정해 줘야 하는데요, 그렇지 않으면 인증 에러가 발생합니다 (line #4-6).

https://gist.github.com/justinyoo/948385359cc739a48ad5afdf07db932e?file=02-disable-secret-http-trigger-03.cs&highlights=4-6

시크릿 이름을 이미 알고 있기 때문에 아래와 같이 직접 이름을 호출해서 모든 시크릿 버전을 가져옵니다. 단, 활성화 된 것만 가져오면 되므로 아래와 같이 `WhereAwait` 구문으로 필터링을 하고 (line #5), 또한 `OrderByDescendingAwait` 구문을 이용해 시간의 역순으로 정렬해서 가장 최근 것이 맨 앞으로 오게끔 처리합니다 (line #6).

https://gist.github.com/justinyoo/948385359cc739a48ad5afdf07db932e?file=02-disable-secret-http-trigger-04.cs&highlights=5,6

만약 해당 시크릿에는 활성화된 버전이 없다면, 더이상 처리할 것이 없으므로 `AcceptedResult` 인스턴스를 반환하는 것으로 펑션을 끝냅니다.

https://gist.github.com/justinyoo/948385359cc739a48ad5afdf07db932e?file=02-disable-secret-http-trigger-05.cs

기본적으로 로테이션 관리를 하기 위해서는 버전이 최소 두 개가 필요하므로, 만약 `count` 값이 주어지지 않았다면 `2`를 기본값으로 해서 `count` 값을 초기화시킵니다.

https://gist.github.com/justinyoo/948385359cc739a48ad5afdf07db932e?file=02-disable-secret-http-trigger-06.cs

현재 활성화된 시크릿 버전이 주어진 카운트 값 보다 많지 않다면 더이상 처리할 것이 없으므로 마찬가지로 `AcceptedResult` 인스턴스를 반환하고 펑션을 끝냅니다.

https://gist.github.com/justinyoo/948385359cc739a48ad5afdf07db932e?file=02-disable-secret-http-trigger-07.cs

이제 남은 시크릿 버전을 대상으로 비활성화 처리를 해야 합니다. 주어진 `count` 값 만큼의 최신 버전을 건너뛰고 그 다음부터 처리합니다 (line #2). 그리고 Enabled 값을 false로 변경하고 (line #7), 업데이트합니다 (line #9).

https://gist.github.com/justinyoo/948385359cc739a48ad5afdf07db932e?file=02-disable-secret-http-trigger-08.cs&highlights=2,7,9

마지막으로 처리 결과를 저장한 변수를 응답 개체에 실어 반환합니다.

https://gist.github.com/justinyoo/948385359cc739a48ad5afdf07db932e?file=02-disable-secret-http-trigger-09.cs

이렇게 해서 애저 펑션 쪽의 구현은 끝났습니다. 이제 이를 애저로 배포하고 난 후 로직 앱에 연동시켜 보겠습니다.


## 애저 로직 앱과 애저 펑션 연동 ##

앞서 만들어 둔 애저 로직 앱 인스턴스에 아래와 같이 HTTP 액션과 응답 액션을 추가합니다. 애저 펑션 엔드포인트를 호출할 때 `ObjectName` 값과 `2`를 라우팅 파라미터로 추가한 것이 보일 겁니다.

![로직 앱 추가 액션][image-07]

이제 애저 키 저장소로부터 애저 이벤트그리드, 로직 앱, 애저 펑션을 이용한 종단간 프로세스 통합 작업은 끝났습니다. 실제로 이 워크플우를 실행시켜 볼까요?


## 종단간 테스트 &ndash; 애저 키 저장소 시크릿 버전 추가 ##

앞서 구현한 모든 통합 워크플로우를 실행시키기 위해서는 애저 키 저장소 인스턴스에 새 시크릿 버전을 추가해 보면 됩니다.

![애저 키 저장소 시크릿 버전 리스트][image-08]

아래와 같이 애저 키 저장소 인스턴스에 새 시크릿 버전을 추가해 보겠습니다.

![애저 키 저장소 새 시크릿 버전 추가][image-09]

아래와 같이 새 버전이 추가된 것이 보이나요?

![애저 키 저장소 새 시크릿 버전 추가 결과][image-10]

그리고 새 버전이 추가됐을 때 이벤트 그리드를 통해 로직 앱으로 받은 이벤트 데이터는 아래와 같습니다. `ObjectName` 값과, 새로 생성된 시크릿 버전 값이 위와 같은 것을 확인하셨나요?

![로직 앱 실행 결과][image-11]

이 로직 앱을 통해 애저 펑션을 호출하고 해당 시크릿의 로테이션 처리 결과가 적용된 애저 키 저장소를 살펴 보면 최신 두 버전을 제외하고 나머지는 모두 비활성화 상태로 바뀐 것이 보입니다.

![애저 키 저장소 시크릿 버전 비활성화][image-12]

<br/>

---

지금까지 [애저 키 저장소][az kv] 인스턴스에 새 [시크릿][az kv secrets] 버전이 추가되는 이벤트를 캡쳐해서, 해당 시크릿만 대상으로 [버전 로테이션 관리][az kv secrets rotation]를 하는 방법을 [애저 이벤트그리드][az evtgrd], [애저 로직 앱][az logapp], [애저 펑션][az fncapp]을 이용해서 구현해 보았습니다. 여러분의 실무에서도 이와 비슷한 방식으로 이벤트를 캡쳐해서 처리하게 하는 방식을 도입해 본다면 꽤 유용할 것입니다.


[image-01]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-01.png
[image-02]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-02-ko.png
[image-03]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-03-ko.png
[image-04]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-04-ko.png
[image-05]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-05-ko.png
[image-06]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-06-ko.png
[image-07]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-07-ko.png
[image-08]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-08-ko.png
[image-09]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-09-ko.png
[image-10]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-10-ko.png
[image-11]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-11-ko.png
[image-12]: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/event-driven-keyvault-secrets-rotation-management-12-ko.png

[post prev 1]: /developerkorea/posts/2021/02/23/keyvault-secrets-rotation-management-in-bulk/
[post prev 2]: /developerkorea/posts/2021/02/02/websub-to-eventgrid-via-cloudevents-and-beyond/
[post prev 3]: /developerkorea/posts/2021/01/19/cloudevents-for-azure-eventgrid-via-azure-functions/

[gh sample]: https://github.com/devkimchi/KeyVault-Reference-Sample/tree/2021-02-24

[az logapp]: https://docs.microsoft.com/ko-kr/azure/logic-apps/logic-apps-overview?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813
[az logapp connectors]: https://docs.microsoft.com/ko-kr/connectors/connectors?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813
[az logapp connectors request]: https://docs.microsoft.com/ko-kr/azure/connectors/connectors-native-reqres?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813
[az logapp connectors evtgrd]: https://docs.microsoft.com/ko-kr/connectors/azureeventgrid/?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813

[az fncapp]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813
[az fncapp trigger http]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=csharp&WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813

[az kv]: https://docs.microsoft.com/ko-kr/azure/key-vault/general/overview?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813
[az kv secrets]: https://docs.microsoft.com/ko-kr/azure/key-vault/secrets/about-secrets?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813
[az kv secrets rotation]: https://docs.microsoft.com/ko-kr/azure/app-service/app-service-key-vault-references?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813#rotation
[az kv evtgrd]: https://docs.microsoft.com/ko-kr/azure/key-vault/general/event-grid-overview?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813
[az kv evtgrd type]: https://docs.microsoft.com/ko-kr/azure/event-grid/event-schema-key-vault?tabs=cloud-event-schema&WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813

[az evtgrd]: https://docs.microsoft.com/ko-kr/azure/event-grid/overview?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813
[az evtgrd delivery auth]: https://docs.microsoft.com/ko-kr/azure/event-grid/security-authentication?WT.mc_id=dotnet-17246-juyoo&ocid=AID3027813

[cncf]: https://cncf.io/

[ce]: https://cloudevents.io/
[ce spec]: https://github.com/cloudevents/spec/tree/v1.0
[ce spec http]: https://github.com/cloudevents/spec/blob/v1.0/http-protocol-binding.md
