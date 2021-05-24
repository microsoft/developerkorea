---
title: "퓨전 개발팀의 파워 앱 개발 실사례"
slug: power-apps-in-fusion-teams
description: "이 포스트에서는 퓨전 개발팀의 실제 파워 앱 개발 사례를 들어 봅니다."
date: "2021-05-18"
image: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-00.png
image_caption: "퓨전 팀에서 파워 앱과 애저 펑션의 조합"
author: justin-yoo
category: Power Platform
tags: azure, fusion-teams, power-apps, azure-functions
canonical_url: https://blog.aliencube.org/ko/2021/05/12/power-apps-in-fusion-teams/
featured: false
---


얼마전 Microsoft에서는 퓨전 개발팀을 위한 [무료 학습 모듈을 공개][pa fusion path]했습니다. 더불어 이를 위한 [전자책도 함께 공개][pa fusion ebook]했는데요, 이를 통해 조직내 퓨전 개발팀의 작동 방식에 대한 아이디어가 실제 어떤 식으로 구현이 되는지를 살펴볼 수 있습니다.

[가트너][gartner fusion]에서는 퓨전 개발팀을 "교차 기능 조직(cross-functional team)으로서 비지니스 가치를 수행하기 위해 다양한 데이터와 기술을 사용한다"라고 정의합니다. 동시에 이 퓨전 개발팀은 대체로 기술적인 의사결정을 IT 조직 외부에서 내리기도 하고, 이 결정의 방향은 IT 조직에서 권장하는 방향과 다르게 가는 경우도 잦습니다. 그리고 이러한 퓨전 개발팀의 리더는 IT 조직 외부에서 오는 경우가 많다고도 하네요. 즉, 기존의 IT 조직 관점에서 흔히 보이는 전문 개발자의 시각보다는 좀 더 넓은 비니지스 관점에서 가치를 충족시키는 서비스 개발이 이루어져야 하는 셈인데요, 그렇다면 실제로 이러한 퓨전 개발팀에서 어떤 제품 혹은 서비스를 개발하고 배포할까요?

이 포스트에서는 Lamna 헬스 케어라는 가상의 회사가 한국에서 VIP 회원들을 위해 운영하는 피트니스 센터에서 회원들이 운동 일지 작성에 사용할 모바일 앱을 [파워 앱][pa]으로 개발하는 일련의 과정에 대해 시리즈로 다루도록 합니다.

* ***퓨전 개발팀의 파워 앱 개발 실사례***
* [파워 앱의 종단간 데이터 흐름 실시간 추적][post 2]
* 파워 앱에 DevOps 적용하기

> 이 포스트에 사용한 백엔드 API 샘플 코드는 이곳 [GitHub 리포지토리][gh sample]에서 다운로드 받을 수 있습니다.


## 시나리오 ##

Lamna 피트니스 센터의 퍼스널 트레이너인 안지민쌤은 자신이 관리하는 회원들에게 운동일지를 항상 수기로 작성해서 줬습니다. 하지만, 굳이 쌤이 관리하는 일지 내용을 다시 수기로 작성해서 건네주는 것이 비효율적이라는 생각에 센터에서 도입한 파워 앱을 이용해서 회원과 공유해보고자 하는 계획을 세웠는데요, 이미 회원의 운동 일지를 저장하는 전산 시스템은 구축이 되어 있는 상태입니다. 그렇다면, 이를 파워 앱에서 사용할 수 있게끔 [사용자 지정 커넥터][pa cuscon]를 만들어 파워 앱에서 사용하기만 하면 될 것이라고 생각하고 있습니다. 파워 앱을 통해 회원의 운동 일지 데이터를 입력 받아 데이터베이스에 저장하는 일련의 과정은 대략 아래와 같은 구조입니다.

![GymLog Architecture][image-01]

* 백엔드에서 사용하는 [애저 펑션][az fncapp]에 [OpenAPI 확장 기능][az fncapp extension openapi]을 설치해서 검색용이성(discoverability)을 높입니다.
* OpenAPI 문서를 이용해 사용자 지정 커넥터를 만들구요,
* 사용자 지정 커넥터를 통해 파워 앱에서 운동 데이터를 백엔드로 보냅니다.
* 백엔드는 비동기식으로 데이터를 다루는 [Pub/Sub 패턴][eip pubsub]을 구현합니다.
* 백엔드 API에서 받아온 데이터는 퍼블리셔 쪽에 계속 쌓이다가 운동이 다 끝나는 시점에 모두 합산(Aggregate)한 데이터를 [애저 서비스 버스][az svcbus]로 보내게 됩니다.
* 서비스 버스로 보낸 데이터는 섭스크라이버 쪽의 애저 펑션에서 받아 최종적으로 [애저 코스모스 DB][az cosdba]로 저장합니다.


## 백엔드 API 개선 ##

안지민쌤은 퍼스널 트레이너 팀장이기도 해서 피트니스 센터의 여러 가지 업무에 대한 자동화 아이디어를 퓨전 개발팀에 제공하고 있습니다. 파워 앱을 개발하기 위해 같은 퓨전 개발팀 내 전문 개발자인 권수빈쌤에게 API 검색이 가능하게끔 해달라는 요청을 했고, 이를 받아들여 권수빈쌤은 애저 펑션에 OpenAPI 기능을 추가했습니다. 이를 위해 가장 먼저 [NuGet 패키지 라이브러리][nuget openapi]를 애저 펑션 프로젝트에 추가했습니다.

```bash
dotnet add package Microsoft.Azure.WebJobs.Extensions.OpenApi --prerelease
```

그리고 기존의 API에 몇 가지 데코레이터만 추가해서 API 엔드포인트를 OpenAPI 문서에 포함시키는 작업 정도만 수행했습니다. 아래 코드는 맨 처음 운동 루틴을 시작할 때 사용하는 API에 대한 내용인데요, OpenAPI와 상관없는 부분은 제외시켰으므로, 만약 해당 API의 전체 코드를 보고 싶다면 [이 링크][gh sample api routine]를 클릭하면 됩니다.

https://gist.github.com/justinyoo/1cb47ec0dad64609d26f1fa69a75b60d?file=01-create-routine.cs&highlights=1-7

위와 같이 OpenAPI 설정을 추가하고 난 후 배포해 보면 아래와 같이 Swagger UI 화면이 나타납니다.

![Publisher Swagger UI][image-02]

위에 추가한 확장 기능 라이브러리는 OpenAPI 스펙 V2 (Swagger)와 V3를 모두 지원하므로, `https://<function_app_name>.azurewebsites.net/api/swagger.json` 으로 접속하면 설정에 따라 V2 문서 혹은 V3 문서를 렌더링할 수 있습니다. 위 그림은 V3 문서를 렌더링하는 것으로 되어 있죠?


## 사용자 지정 커넥터 생성 ##

앞서의 작업을 통해 애저 펑션으로 만들어진 API는 OpenAPI 확장 기능에서 제공하는 실시간 OpenAPI 문서 생성 기능을 통해 검색용이성을 높였으니, 이를 통해 파워 앱 쪽에서 사용자 지정 커넥터를 이용해 API에 접근할 수 있게 해야 합니다. 파워 앱은 로우코드 플랫폼이므로 퓨전 팀 안에서 굳이 전문 개발자인 권수빈쌤이 이 사용자 지정 커넥터를 만들어 주지 않더라도 시민 개발자인 안지민쌤도 충분히 작업이 가능한데요, 아래와 같이 파워 앱 스튜디오의 좌측 `사용자 지정 커넥터` 링크를 클릭한 후 우측 상단의 `➕ 새 사용자 지정 커넥터` 버튼을 눌러 `URL에서 OpenAPI 가져오기` 메뉴를 선택하면 됩니다.

![Import OpenAPI from URL][image-03]

아래와 같이 `공개 API의 URL에 붙여넣기` 필드에 애저 펑션에서 자동 생성해 주는 OpenAPI 문서 URL을 붙여넣습니다. 참고로 사용자 지정 커넥터는 현재 OpenAPI v2 스펙으로 작성된 문서만 인식합니다. 따라서, `https://<function_app_name>.azurewebsites.net/api/openapi/v2.json`과 같은 형태의 URL을 호출하면 곧바로 V2 형식의 OpenAPI 문서를 생성해 주니, 이를 그대로 활용하면 되겠네요.

![Import OpenAPI from URL Pop-up][image-04]

그런데, 만약 아래와 같은 에러가 발생하면서 사용자 지정 커넥터 생성에 실패할 수도 있는데, 이는 파워 앱 스튜디오와 애저 펑션 API 인스턴스 사이에 CORS 설정이 빠져 있기 때문입니다.

![Import OpenAPI from URL CORS Error][image-05]

따라서, 애저 펑션 인스턴스 쪽에서 `https://flow.microsoft.com` 사이트에 대한 CORS 설정을 아래와 같이 해 주면 됩니다.

![Azure Functions App CORS][image-06]

CORS 설정이 끝난 후 파워 앱 스튜디오로 돌아와서 다시 사용자 지정 커넥터를 생성하면 이번에는 오류 없이 바로 생성 가능합니다. 이 이후 과정은 [일반적인 사용자 지정 커넥터 생성 과정과 동일][pa cuscon create]하므로 여기서는 생략하겠습니다. 마침내 아래와 같이 사용자 지정 커넥터가 만들어졌네요!

![Custom Connector Created][image-07]


## 인증을 통한 커넥션 생성 ##

안지민쌤이 이 사용자 지정 커넥터를 활용할 수 있으려면, 먼저 인증 절차를 통해서 커넥션을 만들어 놓아야 합니다. 애저 펑션 API 엔드포인트는 API 키로 보호받고 있으므로 이를 이용해 인증하고 커넥션을 만들면 됩니다. 아래와 같이 `➕` 버튼을 클릭합니다.

![New Connection][image-08]

애저 펑션에서 제공하는 API 인증 키 값을 입력한 후 `만들기` 버튼을 클릭합니다.

![API Key Auth][image-09]

그러면, 아래와 같이 커넥션이 만들어졌고, 이제 파워 앱 안에서 이 사용자 지정 커넥터를 자유자재로 사용할 수 있게 됐습니다!

![Connection Created][image-10]


## 파워 앱에서 사용자 지정 커넥터 사용 ##

이제 안지민쌤은 본인이 관리하는 회원들의 운동 일지를 수기로 작업하는 대신 파워 앱을 이용해 작성하면 되는데요, 아래와 같이 파워 앱 캔버스 화면에서 사용자 지정 커넥터를 추가합니다.

![Custom Connector in Power Apps][image-11]

그리고 앱을 개발했습니다! 이제 회원들의 운동일지를 앱으로 관리할 수 있게 됐네요. 아래는 파워 앱을 모바일에서 사용할 때의 스크린샷입니다.

![Power Apps in Action #1][image-12]
![Power Apps in Action #2][image-13]

마지막으로 이렇게 기록한 운동 일지는 [애저 코스모스 DB][az cosdba]에 비동기식으로 저장됩니다.

![Gym Logs in Cosmos DB][image-14]

안지민쌤의 회원들은 이제 좀 더 손쉽게 스스로 운동 일지를 작성할 수 있도록 앱을 제공 받았습니다.

> 만약 이 GymLogs 앱을 사용해 보고 싶다면 [이 링크][gh sample app]에서 다운로드 받아 자신의 파워 앱 환경에 업로드해서 사용하면 됩니다.

---

지금까지 퓨전 개발팀에서

* 현장 전문가는 시민 개발자로서 [파워 앱][pa]을 개발하고,
* 전문 개발자는 이 파워 앱의 기능을 풍부하게 해 주는 커스텀 커넥터를 위한 백엔드 API를 OpenAPI 형식으로 공개하는 형태로

협업을 하는 과정에 대해 알아 보았습니다. 이를 통해 결국 Lamna 피트니스 센터의

* 고객들은 좀 더 체계적인 운동 일지를 작성할 수 있게 되었고,
* 이 데이터를 바탕으로 트레이너들은 좀 더 회원별로 개인화된 운동 루틴을 작성할 수 있게 되었습니다.

이렇게 함으로써 회원과 피트니스 센터 모두에게 좀 더 나은 서비스를 제공할 수 있는 기반이 다져진 셈이죠. [다음 포스트][post 2]에서는 [애저 모니터][az monitor] 서비스를 이용해 파워 앱에서 데이터 저장소까지 데이터가 이동하면서 거쳐가는 경로들을 추적하는 과정에 대해 알아보기로 하겠습니다.


## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][az account free]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt msdevkr]


[az account free]: https://azure.microsoft.com/ko-kr/free/?WT.mc_id=power-27849-juyoo&ocid=AID3027813
[ms learn]: https://docs.microsoft.com/ko-kr/learn/?WT.mc_id=power-27849-juyoo&ocid=AID3027813
[yt msdevkr]: https://www.youtube.com/c/microsoftdeveloperkorea


[image-01]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-01.png
[image-02]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-02.png
[image-03]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-03-ko.png
[image-04]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-04-ko.png
[image-05]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-05-ko.png
[image-06]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-06-ko.png
[image-07]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-07-ko.png
[image-08]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-08-ko.png
[image-09]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-09-ko.png
[image-10]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-10-ko.png
[image-11]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-11.png
[image-12]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-12.png
[image-13]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-13.png
[image-14]: https://sa0blogs.blob.core.windows.net/aliencube/2021/05/power-apps-in-fusion-teams-14.png

[post 1]: /developerkorea/posts/2021/05/18/power-apps-in-fusion-teams/
[post 2]: /developerkorea/posts/2021/05/25/tracing-end-to-end-data-from-power-apps-to-azure-cosmos-db/

[gh sample]: https://github.com/aliencube/GymLog
[gh sample api routine]: https://github.com/aliencube/GymLog/blob/main/src/GymLog.FunctionApp/Triggers/RoutineHttpTrigges.cs
[gh sample app]: https://github.com/aliencube/GymLog/blob/main/packages/GymLogs.zip

[pa fusion path]: https://docs.microsoft.com/ko-kr/learn/paths/transform-business-applications-with-fusion-development/?WT.mc_id=power-27849-juyoo&ocid=AID3027813
[pa fusion ebook]: https://docs.microsoft.com/ko-kr/powerapps/guidance/fusion-dev-ebook/?WT.mc_id=power-27849-juyoo&ocid=AID3027813

[gartner fusion]: https://blogs.gartner.com/hank-barnes/2021/03/30/fusion-teams-a-critical-area-for-vendors-to-develop-understanding/

[eip pubsub]: https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html

[az fncapp]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview?WT.mc_id=power-27849-juyoo&ocid=AID3027813
[az fncapp extension openapi]: https://github.com/Azure/azure-functions-openapi-extension

[az svcbus]: https://docs.microsoft.com/ko-kr/azure/service-bus-messaging/service-bus-messaging-overview?WT.mc_id=power-27849-juyoo&ocid=AID3027813
[az cosdba]: https://docs.microsoft.com/ko-kr/azure/cosmos-db/introduction?WT.mc_id=power-27849-juyoo&ocid=AID3027813

[az monitor]: https://docs.microsoft.com/ko-kr/azure/azure-monitor/overview?WT.mc_id=power-27849-juyoo&ocid=AID3027813

[pa]: https://powerapps.microsoft.com/ko-kr/?WT.mc_id=power-27849-juyoo&ocid=AID3027813
[pa cuscon]: https://docs.microsoft.com/ko-kr/connectors/custom-connectors/?WT.mc_id=power-27849-juyoo&ocid=AID3027813
[pa cuscon create]: https://docs.microsoft.com/ko-kr/connectors/custom-connectors/define-openapi-definition?WT.mc_id=power-27849-juyoo&ocid=AID3027813

[nuget openapi]: https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.OpenApi/
