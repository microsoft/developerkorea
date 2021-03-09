---
title: "애저 펑션에 Open API 활성화 시키기"
slug: enabling-openapi-on-azure-functions
description: "이 포스트에서는 애저 펑션에서 Open API 기능을 활성화 시키는 방법에 대해 알아 봅니다."
date: 2021-03-09
image: https://sa0blogs.blob.core.windows.net/msdevkr/2021/03/enabling-openapi-on-azure-functions-00.png
image_caption: 애저 펑션과 Open API
author: justin-yoo
category: Azure
tags: azure-functions, openapi
canonical_url:
featured: false
---

[애저 펑션][az fncapp]에서는 현재 [v1 런타임에 대해서만 프리뷰 형태로 Open API 지원][az fncapp v1 openapi]을 하고 있습니다. 하지만, 애저 펑션의 현재 런타임 버전은 v3인데, 아직까지 v2 버전 이후로 공식적으로 Open API 기능을 지원하지 않습니다. 다행히도 [이 확장 기능][gh openapi]을 사용하면 애저 펑션에 [Open API][openapi] 기능을 활성화 시킬 수 있는데, 이 포스트에서는 이 확장 기능을 사용하는 방법에 대해 알아보기로 하겠습니다.

> **NOTE**: 현재 이 [확장 기능][gh openapi]은 프리뷰 상태로, 정식 출시되기 전입니다. 미리 사용해 보시고, 피드백을 [깃헙 이슈][gh openapi issues]에 남겨주세요!


## 애저 펑션 앱 생성하기 ##

가장 먼저 애저 펑션 프로젝트를 생성합니다.

https://gist.github.com/justinyoo/2516ec59b204e2bbf85181620f1d0aea?file=01-func-init.sh

그 다음에 [HTTP 트리거 펑션][az fncapp trigger http]을 하나 생성합니다.

https://gist.github.com/justinyoo/2516ec59b204e2bbf85181620f1d0aea?file=02-func-new.sh

그러면 아래와 같이 기본 HTTP 트리거 펑션 코드가 만들어 집니다.

https://gist.github.com/justinyoo/2516ec59b204e2bbf85181620f1d0aea?file=03-default-http-trigger.cs

이제 아래 명령어를 통해 이 애저 펑션 앱을 한 번 실행시켜 보겠습니다.

https://gist.github.com/justinyoo/2516ec59b204e2bbf85181620f1d0aea?file=04-func-start.sh

예상한 바와 같이 애저 펑션의 엔드포인트는 `/api/DefaultHttpTrigger` 하나만 보입니다.

![애저 펑션 엔드포인트][image-01]


## Open API 기본 기능 추가하기 ##

이제 Open API 기능을 활성화 시키기 위해 NuGet 패키지를 설치할 차례입니다. 아래 명령어를 통해 [확장 기능 패키지 라이브러리][nuget openapi]를 설치합니다.

https://gist.github.com/justinyoo/2516ec59b204e2bbf85181620f1d0aea?file=05-dotnet-add-package.sh

> **NOTE**: 이 포스트를 작성하는 시점에서 이 확장 기능 패키지 라이브러리의 버전은 `0.5.1-preview` 입니다.

이제 다시 한 번 애저 펑션 앱을 실행시켜 보겠습니다. 그러면 추가적인 엔드포인트가 보입니다.

![애저 펑션 Open API용 추가 엔드포인트][image-02]

이 추가 엔드포인트를 통해 Open API 기능을 확인할 수 있습니다. 웹 브라우저를 통해 맨 아래에 있는 `http://localhost:7071/api/swagger/ui` 주소로 접속해 보겠습니다. 아래와 같이 Swagger UI 페이지를 볼 수 있습니다.

![Swagger UI 페이지 - 엔드포인트 안보임][image-03]

그런데, 위 페이지를 보면 분명히 이 앱에서는 `/api/DefaultHttpTrigger`라는 엔드포인트가 있는데, 이 UI에서는 없다고 나옵니다. 어떻게 된 일일까요? 아직 해당 엔드포인트에 설정을 하지 않은 상태라서 그렇습니다. 이제 아래와 같이 Open API 관련 설정을 엔드포인트에 추가해 보도록 하겠습니다 (line #3-5).

https://gist.github.com/justinyoo/2516ec59b204e2bbf85181620f1d0aea?file=06-default-http-trigger.cs&highlights=3-5

* `OpenApiOperation`: Open API 스펙에 따르면 모든 엔드포인트는 고유의 Operation ID 값을 갖고 있어야 합니다. 이를 정의하는 데코레이터입니다.
* `OpenApiParameter`: 이 엔드포인트로 파라미터를 보내는 방법에 대해 정의합니다. 여기서는 쿼리스트링으로 `name`이라는 파라미터를 통해 값을 전송합니다.
* `OpenApiResponseWithBody`: 이 엔드포인트로 HTTP 요청을 보낼 때 받을 수 있는 응답 개체의 형식을 정의합니다. 여기서는 `text/plain` 형식으로 문자열을 반환합니다.

위와 같이 설정한 후 다시 펑션 앱을 실행시켜 보면 아래와 같이 UI 페이지에서 엔드포인트가 보입니다!

![Swagger UI 페이지 - 엔드포인트 보임][image-04]


## Open API 보안 기능 추가하기 ##

일반적으로 API는 부정한 방법으로 접근하는 것을 방지하기 위해 보안 설정을 하죠. 그렇다면, 이 확장 기능으로는 어떻게 이 보안 설정을 정의할까요? 아래 코드를 한 번 보겠습니다 (line #7).

https://gist.github.com/justinyoo/2516ec59b204e2bbf85181620f1d0aea?file=07-default-http-trigger.cs&highlights=7

* `OpenApiSecurity`: 애저 펑션은 기본적으로 API Key 값을 통해 보안을 설정할 수 있습니다. 이 키 값을 쿼리스트링으로 보낼 때는 `code`라는 파라미터로, 요청 헤더를 통해 보낼 때는 `x-functions-key`를 통해 보내는데요, 여기서는 쿼리스트링으로 보내는 것으로 정의합니다.

이렇게 한 후, 다시 펑션 앱을 실행시켜 보겠습니다. 아래 그림에 보면 전에 보이지 않던 `Authorize 🔓` 버튼이 보입니다.

![Swagger UI 페이지 - 자물쇠 열림][image-05]

이 버튼을 클릭해 보면 아래와 같은 팝업 창이 생기는데요, 여기에 애저 펑션의 API Key 값을 입력합니다. 현재는 로컬 개발 환경에서 실행시키고 있으니, 아무 값이나 넣어도 상관 없습니다. 쿼리스트링에 `code`라는 파라미터로 추가하는 것이 보이시나요?

![Swagger UI 페이지 - 인증 팝업][image-06]

인증을 하고 나면 아래와 같이 자물쇠 모양이 잠긴 것을 확인합니다.

![Swagger UI 페이지 - 자물쇠 닫힘][image-07]

그리고, UI 상에서 이 엔드포인트를 실행시켜 볼까요? `name` 필드에 아무 값이나 넣고 실행시켜 보면 아래와 같습니다. 쿼리스트링에 `code=abcde` 값이 포함된 것을 볼 수 있습니다.

![Swagger UI 페이지 - API 실행][image-08]

<br />

---

이렇게 해서 애저 펑션에 Open API 기능을 활성화 시키는 방법에 대해 알아 봤습니다. 한 가지 문제가 있다면, 이 확장 기능은 런타임 버전 v2 이상만을 지원한다는 건데요, 여러 가지 이유로 v1 런타임을 사용해야 하는 경우에는 어떻게 해야 할까요? 다음 포스트에서는 애저 펑션 런타임 v1을 지원하는 방법에 대해 알아보도록 하겠습니다.


## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][az account free]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt msdevkr]


[image-01]: https://sa0blogs.blob.core.windows.net/msdevkr/2021/03/enabling-openapi-on-azure-functions-01.png
[image-02]: https://sa0blogs.blob.core.windows.net/msdevkr/2021/03/enabling-openapi-on-azure-functions-02.png
[image-03]: https://sa0blogs.blob.core.windows.net/msdevkr/2021/03/enabling-openapi-on-azure-functions-03.png
[image-04]: https://sa0blogs.blob.core.windows.net/msdevkr/2021/03/enabling-openapi-on-azure-functions-04.png
[image-05]: https://sa0blogs.blob.core.windows.net/msdevkr/2021/03/enabling-openapi-on-azure-functions-05.png
[image-06]: https://sa0blogs.blob.core.windows.net/msdevkr/2021/03/enabling-openapi-on-azure-functions-06.png
[image-07]: https://sa0blogs.blob.core.windows.net/msdevkr/2021/03/enabling-openapi-on-azure-functions-07.png
[image-08]: https://sa0blogs.blob.core.windows.net/msdevkr/2021/03/enabling-openapi-on-azure-functions-08.png

[az account free]: https://azure.microsoft.com/ko-kr/free/?WT.mc_id=dotnet-19697-juyoo&ocid=AID3027813
[ms learn]: https://docs.microsoft.com/ko-kr/learn/?WT.mc_id=dotnet-19697-juyoo&ocid=AID3027813
[yt msdevkr]: https://www.youtube.com/channel/UCdgR-b2t7Byu_UGrHnu-T0g

[gh openapi]: https://github.com/Azure/azure-functions-openapi-extension
[gh openapi issues]: https://github.com/Azure/azure-functions-openapi-extension/issues

[nuget openapi]: https://www.nuget.org/packages/Microsoft.Azure.WebJobs.Extensions.OpenApi/

[az fncapp]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview?WT.mc_id=dotnet-19697-juyoo&ocid=AID3027813
[az fncapp v1 openapi]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-openapi-definition?WT.mc_id=dotnet-19697-juyoo&ocid=AID3027813
[az fncapp trigger http]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=csharp&WT.mc_id=dotnet-19697-juyoo&ocid=AID3027813

[openapi]: https://www.openapis.org/
