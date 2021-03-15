---
title: "애저 펑션 프록시 기능을 이용한 Open API 확장 기능 하위 호환성 유지하기"
slug: openapi-extension-to-support-azure-functions-v1
description: "이 포스트에서는 애저 펑션의 Open API 확장 기능이 직접 지원하지 않는 v1 런타임을 애저 펑션의 프록시 기능을 이용해서 지원할 수 있는 방법에 대해 알아봅니다."
date: 2021-03-16
image: https://sa0blogs.blob.core.windows.net/aliencube/2020/11/openapi-extension-to-support-azure-functions-v1-00.png
image_caption: 애저 펑션 v1 런타임과 Open API
author: justin-yoo
category: Azure
tags: azure-functions, azure-functions-proxy, openapi, backward-compatibility
canonical_url: https://blog.aliencube.org/ko/2020/11/11/openapi-extension-to-support-azure-functions-v1/
featured: false
---


[지난 포스트][post prev]에서 [애저 펑션][az fncapp]에 데코레이터를 추가해서 Open API 문서를 자동으로 생성하는 Open API 확장 기능과 관련한 [오픈 소스][gh openapi] 프로젝트와 [NuGet 라이브러리][nuget openapi]를 소개했습니다. 이 라이브러리는 현재 애저 펑션 런타임 버전 v2 부터 지원합니다. 하지만, 실제 현업에서는 여전히 여러 이유로 v1 런타임을 사용하는 곳도 많습니다. 그렇다면 이런 경우에는 이 확장 기능을 사용할 수 없는 걸까요? 그럴 수도 있고 아닐 수도 있습니다. Open API 확장 기능을 직접 v1 런타임에 적용시킬 수는 없지만, [애저 펑션 프록시 기능][az fncapp proxy]을 이용하면 가능한데요, 이 포스트에서는 이 트릭에 대해 알아보기로 하겠습니다.


## 레거시 애저 펑션 ##

레거시 엔터프라이즈 애플리케이션의 경우에는 참조 라이브러리 의존성 때문에 v1 런타임으로만 애저 펑션을 구성해야 하는 경우가 왕왕 있습니다. 여기 레거시 애저 펑션 엔드포인트가 아래와 같은 형태로 구성되어 있다고 가정하겠습니다.

https://gist.github.com/justinyoo/2b0b286bbe3e727e17423047cd97f86e?file=01-v1-legacy.cs

애저 펑션 런타임 v1의 경우, [Newtonsoft.Json][nuget jsonnet] 버전 9.0.1에 고정되어 있고, 이를 변경하기가 굉장히 어렵습니다. 그런데, 예를 들어 이 `MyReturnObject` 클라스가 Newtonsoft.Json 버전 10.0.1 이상에 대한 의존성을 갖고 있다면, 이 경우에는 이 [Open API 확장 기능 라이브러리][gh openapi]를 사용할 수 없겠지요?


## Open API 문서 정의용 애저 펑션 만들기 ##

이런 경우에는 [애저 펑션 프록시 기능][az fncapp proxy]으로 한 번 감싸주면 완벽하지는 않더라도 동일한 개발자 경험을 제공함으로써 문제를 해결할 수 있습니다. 먼저 Open API 정의용 애저 펑션을 v3 런타임으로 아래와 같이 만들어 보겠습니다. 펑션 앱 이름은 `MyV1ProxyFunctionApp`으로 두고 (line #1), 기본적으로 모든 펑션 기능은 레거시 v1 앱과 동일하게 작성합니다 (line #3-7). 하지만, 이 펑션은 그저 프록시 용도이므로 실제 펑션이 하는 일은 없으니 간단하게 OK 결과만 반환하게끔 설정합니다 (line #10).

https://gist.github.com/justinyoo/2b0b286bbe3e727e17423047cd97f86e?file=02-v1-proxy.cs&highlights=1,3-7,10

이제 Open API 확장 기능 라이브러리를 설치했다면, 데코레이터를 추가할 차례입니다. 아래와 같이 `FunctionName(...)` 데코레이터 위에 Open API 메타데이터 관련 데코레이터를 추가합니다 (line #5-9).

https://gist.github.com/justinyoo/2b0b286bbe3e727e17423047cd97f86e?file=03-v1-proxy-openapi.cs&highlights=5-9

여기까지 하고 이 프록시 애저 펑션 앱을 실행시키면 성공적으로 Swagger UI 화면을 볼 수 있습니다. 하지만, 이 앱은 Swagger UI 화면만 보여줄 뿐 실제로 동작하는 화면은 아니므로 추가로 작업을 해 줄 것이 있습니다. 바로 프록시 기능이죠.


## 레거시 애저 펑션 프록시 추가하기 ##

아래와 같이 `proxies.json` 파일을 프로젝트 루트 폴더에 만듭니다. 레거시 펑션과 프록시 펑션은 동일한 엔드포인트를 갖게끔 만들어 놨기 때문에 (line #6, 11) 사용자 입장에서는 마치 웹서버가 바뀌는 정도의 차이만 느낄 뿐 개발 경험은 동일하게 유지할 수 있습니다. 또한 쿼리스트링과 요청 헤더 역시도 동일하게 레거시 펑션 앱으로 전달합니다 (line #13-14).

https://gist.github.com/justinyoo/2b0b286bbe3e727e17423047cd97f86e?file=04-proxies.json&highlights=6,11,13-14

프록시 펑션을 배포할 때 이 `proxies.json` 파일도 함께 배포해야 하므로 아래와 같이 `.csproj` 파일을 수정해 줍니다 (line #10-12).

https://gist.github.com/justinyoo/2b0b286bbe3e727e17423047cd97f86e?file=05-v1-proxy.csproj&highlights=10-12

이렇게 한 후 이 프록시 펑션을 로컬에서 실행시켜 보거나 애저로 배포한 후 프록시 API 엔드포인트로 접속해 볼까요? 이제 원하는 Open API 문서도 생성할 수도 있고, 실제 레거시 API도 프록시를 통해 실행시킬 수도 있습니다.

<br/>

---

지금까지 [애저 펑션 프록시][az fncapp proxy] 기능을 이용해서 v1 런타임으로만 구성 가능한 레거시 애저 펑션에 [Open API 확장 기능][gh openapi]을 구현하는 방법에 대해 알아 보았습니다. 이 방법의 단점이라면 레거시 API를 한 번 호출할 때 프록시를 거쳐가므로 비용이 두 배로 들어간다는 것인데, 이 부분은 실제 엔터프라이즈 애플리케이션 아키텍팅의 관점에서 신중히 결정해서 결정하시면 됩니다.


## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][az account free]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt msdevkr]


[az account free]: https://azure.microsoft.com/ko-kr/free/?WT.mc_id=dotnet-20715-juyoo&ocid=AID3027813
[ms learn]: https://docs.microsoft.com/ko-kr/learn/?WT.mc_id=dotnet-20715-juyoo&ocid=AID3027813
[yt msdevkr]: https://www.youtube.com/channel/UCdgR-b2t7Byu_UGrHnu-T0g

[post prev]: /developerkorea/posts/2021/03/09/enabling-openapi-on-azure-functions/

[gh openapi]: https://github.com/Azure/azure-functions-openapi-extension

[nuget openapi]: https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.OpenApi/
[nuget jsonnet]: https://www.nuget.org/packages/Newtonsoft.Json/

[az fncapp]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview?WT.mc_id=dotnet-20715-juyoo&ocid=AID3027813
[az fncapp proxy]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-proxies?WT.mc_id=dotnet-20715-juyoo&ocid=AID3027813
