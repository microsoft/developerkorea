---
title: "애저 펑션에 Open API 확장 기능을 이용해 파워 플랫폼용 커스텀 커넥터 곧바로 생성하기"
slug: creating-custom-connector-from-azure-functions-with-swagger
description: "이 포스트에서는 애저 펑션 API에 Open API 확장 기능을 통합해서 파워 플랫폼에 사용할 커스텀 커넥터를 생성하는 방법에 대해 알아봅니다."
date: 2021-02-09
image: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-00.png
image_caption: 파워 플랫폼에 Open API 확장 기능을 연동시킨 애저 펑션 API로 커스텀 커넥터를 생성해서 연결
author: justin-yoo
category: Power Platform
tags: azure-functions, power-platform, openapi, custom-connector
canonical_url: https://blog.aliencube.org/ko/2020/07/15/creating-custom-connector-from-azure-functions-with-swagger/
featured: false
---


[애저 펑션을 위한 Open API 확장 기능][gh openapi]을 사용하면 좋은 점 중에 하나가 바로 [애저 펑션][az func]으로 API를 개발할 때 이 API의 발견가능성(discoverability)을 높여준다는 것입니다. 따라서, 이를 이용하면 굉장히 손쉽게 [애저 API 관리도구][az apim]에 연동시킬 수 있습니다. 또한 [애저 로직 앱][az logapp]이나 [파워 플랫폼][power platform] 에서 사용하는 [커스텀 커넥터][az cuscon] 역시도 쉽게 생성할 수 있는데요, 이 포스트에서는 이 Open API 확장 기능을 이용해 애저 펑션에 Open API 문서를 통합한 후, 이를 이용해 커스텀 커넥터를 만들어 보는 방법에 대해 알아봅니다.


## 샘플 애저 펑션 코드 ##

우선 기본적인 뼈대만 갖춘 애저 펑션 샘플 코드는 아래와 같습니다. `/feeds/items`와 `/feeds/item` 이라는 두 개의 엔드포인트를 나타냅니다 (line #7, 15).

https://gist.github.com/justinyoo/2b4bc731ff8f2cdb5e80e28bd7dff9e7?file=01-feed-reader.cs&highlights=7,15

이를 실행시키면 당연하겠지만, 아래와 같이 두 개의 엔드포인트를 확인할 수 있습니다.

![애저 펑션 엔드포인트][image-01]


## NuGet 패키지 설치 ##

애저 펑션에 Open API 문서를 손쉽게 생성해 주는 NuGet 패키지를 설치합니다.

https://gist.github.com/justinyoo/2b4bc731ff8f2cdb5e80e28bd7dff9e7?file=02-install-nuget-package-2.sh


## 보일러플레이트 코드 설치 ##

사실, 위 NuGet 패키지를 설치하면 보일러플레이트 코드가 자동으로 설치가 됩니다. 따라서, 이 부분은 딱히 고민할 부분이 없습니다. 앱을 빌드하고 실행시켜볼까요? 아래 그림과 같이 추가 엔드포인트가 보일텐데, 이 세 엔드포인트가 바로 Open API와 관련한 것들입니다.

![애저 펑션 Open API 엔드포인트][image-02]

이제 이 중에서 `http://localhost:7071/api/swagger/ui`를 브라우저에서 실행시켜 보면 아래와 같습니다.

![엔드포인트 없는 애저 펑션 Swagger UI 페이지][image-03]

일단 Swagger UI 페이지는 나왔지만, 아직 엔드포인트를 설정하지 않았기 때문에 자세한 내용은 보이질 않습니다.


## Open API 확장을 위한 데코레이터 지정 ##

이제 아래와 같이 각각의 엔드포인트에 데코레이터를 이용해 Open API 설정을 해 보겠습니다. `OpenApiOperationAttribute`, `OpenApiSecurityAttribute`, `OpenApiRequestBodyAttribute`, `OpenApiResponseWithBodyAttribute` 등의 데코레이터를 사용했습니다 (line #1-4, 13-16).

https://gist.github.com/justinyoo/2b4bc731ff8f2cdb5e80e28bd7dff9e7?file=08-add-decorators-2.cs&highlights=1-4,13-16

이렇게 컴파일 한 후, 다시 펑션 앱을 실행시켜 보면 아래와 같이 Swagger UI 페이지가 제대로 보입니다.

![엔드포인트 포함한 애저 펑션 Swagger UI 페이지 #1][image-04]

이렇게 애저 펑션에 Open API 익스텐션을 추가해서 Swagger UI 페이지를 붙이는 것 까지 살펴봤습니다. 이를 애저로 배포한 후 다시 Swagger UI 페이지에 접속해 볼까요?

![엔드포인트 포함한 애저 펑션 Swagger UI 페이지 #1][image-05]

이제 배포가 끝났으니 실제 커스텀 커넥터를 만들기 위한 다음 단계로 넘어가도록 하겠습니다.


## 커스텀 커넥터 생성 ##

커스텀 커넥터는 한 번 만들어 놓으면 [파워 오토메이트][power automate]와 [파워 앱스][power apps] 어디서든 사용할 수 있습니다. 따라서 여기서는 파워 오토메이트에서 커스텀 커넥터를 만들어 보기로 합니다. 먼저 아래와 같이 애저 펑션에서 제공하는 Swagger 문서의 URL을 지정합니다.

![파워 오토메이트 커스텀 커넥터 #1][image-06]

그런데, 가끔 아래와 같이 잘 안될 때가 있습니다. 이것은 현재 알려진 버그인데요, 괜찮습니다.

![파워 오토메이트 커스텀 커넥터 #2][image-07]

그럴 땐 당황하지 말고, Swagger 문서를 저장한 후 직접 업로드합니다.

![파워 오토메이트 커스텀 커넥터 #3][image-08]

이렇게 하면 그 다음부터는 그냥 자동으로 진행됩니다. 애초에 이 Open API 익스텐션이 바로 이 커스텀 커넥터를 염두에 두고 만든 것이어서 문제없이 진행됩니다. 아래와 같이 `✅ Create Connector` 버튼을 눌러 마무리합니다.

![파워 오토메이트 커스텀 커넥터 #4][image-09]

이제 커스텀 커넥터가 제대로 작동하는지 테스트를 해 볼 차례입니다. 아래 그림과 같이 `4. Test` 탭에서 커스텀 커넥터를 연결합니다.

![파워 오토메이트 커스텀 커넥터 #5][image-10]

그러면 아래 그림과 같이 애저 펑션 API 키 값을 입력하라는 표시가 나타나는데요, 여기서 API 키 값을 입력한 후 연결합니다.

![파워 오토메이트 커스텀 커넥터 #6][image-11]

커스텀 커넥터에 성공적으로 커넥션이 만들어지면, 이제 아래와 같이 실제로 테스트를 진행합니다. 아래 그림의 입력창은 바로 Swagger 문서에 정의된 요청 객체의 형식을 그대로 따라갑니다. 필요한 데이터를 입력하고 아래 `Test Operation` 버튼을 눌러보겠습니다.

![파워 오토메이트 커스텀 커넥터 #7][image-12]

그러면 아래와 같이 테스트가 성공적으로 수행된 것이 보입니다.

![파워 오토메이트 커스텀 커넥터 #8][image-13]

이제 커스텀 커넥터를 생성했으니, 파워 오토메이트를 하나 만들어 보겠습니다.


## 커스텀 커넥터를 이용한 파워 오토메이트 플로우 만들기 ##

이번에 만드는 파워 오토메이트 플로우는 파워 앱에서 이용할 것이기 때문에 아래와 같은 순서로 생성합니다. 먼저 `Instant Flow`를 선택합니다.

![파워 오토메이트 커스텀 커넥터 #9][image-14]

그 다음에는 아래 그림과 같이 파워 앱을 트리거로 지정합니다.

![파워 오토메이트 커스텀 커넥터 #10][image-15]

그러면 이제 본격적인 플로우 작성을 위한 디자이너 창이 생겼습니다. 여기서 `➕ New Step` 버튼을 클릭합니다.

![파워 오토메이트 커스텀 커넥터 #11][image-16]

검색 창에 `ATOM`을 입력하면 방금 우리가 생성한 커스텀 커넥터가 보입니다. 그리고, 애저 펑션에서 만든 두 개의 엔드포인트가 나타나는 것을 알 수 있죠? 여기서 피드 아이템 하나만 가져오는 액션을 아래와 같이 선택합니다.

![파워 오토메이트 커스텀 커넥터 #12][image-17]

액션을 선택하면 앞서 커스텀 커넥터를 테스트 할 때와 비슷한 필드 입력 화면이 나타납니다. 동일한 내용을 입력합니다.

![파워 오토메이트 커스텀 커넥터 #13][image-18]

이 플로우의 목적은 방금 커스텀 커넥터로 받아온 유튜브 피드를 소셜미디어에 포스팅하는 것이므로, 다음 액션으로 아래와 같이 트위터에 포스팅하는 액션을 선택합니다.

![파워 오토메이트 커스텀 커넥터 #14][image-19]

그리고 난 후, 앞서 커스텀 커넥터로부터 받아온 데이터를 아래와 같이 트위터 포스트 데이터로 입력합니다.

![파워 오토메이트 커스텀 커넥터 #15][image-20]

이번에는 파워 앱으로 이 플로우의 결과를 넘겨줘야 하니 아래와 같이 응답 객체 액션을 선택합니다.

![파워 오토메이트 커스텀 커넥터 #16][image-21]

그리고, 응답 객체의 본문에는 커스텀 커넥터에서 받아온 응답 객체 전부를 넣습니다.

![파워 오토메이트 커스텀 커넥터 #17][image-22]

여기까지 하면 파워 오토메이트 플로우 작성은 거의 다 끝났습니다. 한 번 테스트를 해 볼까요? 우측 상단의 `Test` 버튼을 클릭해서 아래와 같이 선택한 후 `Save & Test` 버튼을 클릭합니다.

![파워 오토메이트 커스텀 커넥터 #18][image-23]

그러면 다음 화면에서는 이 플로우에서 사용하는 커넥터들이 다 제대로 연결되어 있는지 확인하게 되는데, 다 연결 되었다면 아래 `Continue` 버튼을 눌러 계속 진행합니다.

![파워 오토메이트 커스텀 커넥터 #19][image-24]

모든 것이 잘 진행된다면 아래와 같이 테스트에 성공했다는 메시지를 볼 수 있습니다.

![파워 오토메이트 커스텀 커넥터 #20][image-25]

이제 실제로 워크플로우가 어떻게 진행됐는지 살펴보겠습니다. 모든 액션은 아래와 같이 성공적으로 진행되었습니다. 여기서 응답 객체 데이터를 복사해 놓겠습니다.

![파워 오토메이트 커스텀 커넥터 #21][image-26]

그리고 실제로 트위터에도 성공적으로 포스팅이 된 것을 확인할 수 있습니다.

![파워 오토메이트 커스텀 커넥터 #22][image-27]

이제 응답 객체를 파워앱에서 좀 더 확실하게 인식할 수 있게끔 마지막 설정을 해 줄 차례입니다. 앞서 복사한 응답 객체 데이터를 가지고 JSON 스키마를 설정합니다. 아래 그림의 `Generate from Sample` 버튼을 클릭합니다.

![파워 오토메이트 커스텀 커넥터 #23][image-28]

방금 복사해 놨던 JSON 응답 객체를 붙여넣고 `Done` 버튼을 클릭합니다.

![파워 오토메이트 커스텀 커넥터 #24][image-29]

그러면 JSON 응답객체 스키마가 생성된 것을 확인할 수 있습니다.

![파워 오토메이트 커스텀 커넥터 #25][image-30]

여기까지 한 후 저장하면 파워 오토메이트 플로우 작성은 모두 끝났습니다.


## 파워 앱에 파워 오토메이트 연동하기 ##

이제 파워 앱을 만들어 볼 차례입니다. 이번에 만드는 파워 앱에 앞서 만든 파워 오토메이트를 연결해 보도록 하죠. 우선 새 앱 캔버스를 하나 생성합니다.

![파워 앱스 #1][image-31]

그 다음에 버튼 콘트롤 하나, 라벨 콘트롤 두 개, 이미지 콘트롤 하나를 캔버스에 추가합니다.

![파워 앱스 #2][image-32]

버튼을 눌렀을 때 필요한 액션이 바로 파워 오토메이트를 실행시키는 것입니다. 이 액션을 아래와 같이 연결합니다. 버튼을 클릭한 후 상단의 메뉴 바에서 `Action`을 클릭합니다. 그리고 바로 아래에 있는 `Power Automate`를 선택합니다. 그 다음에 나오는 화면에서 앞서 만들어 둔 파워 오토메이트를 선택하면 됩니다.

![파워 앱스 #3][image-33]

연결이 끝나면 곧바로 함수 창에 어떤 작업을 할 것인지를 물어보는데, 여기에 `ClearCollect(result, AmplifyingaRandomYouTubeContent.Run())` 라고 입력합니다. 여기서 `AmplifyingaRandomYouTubeContent()` 함수는 파워 오토메이트 이름을 의미합니다.

![파워 앱스 #4][image-34]

이제 다른 레이블 콘트롤 두 개와 이미지 콘트롤 한 개에는 이 파워 오토메이트 실행 결과를 표시합니다. 각각의 콘트롤에 아래와 같이 입력합니다.

* 상단 레이블 콘트롤: `First(result).title`
* 하단 레이블 콘트롤: `First(result).link`
* 이미지 콘트롤: `First(result).thumbnailLink`

이렇게 입력한 후 파워 앱을 실행시켜 버튼을 클릭해 볼까요? 그러면 아래와 같은 결과가 나타납니다.

![파워 앱스 #5][image-35]

그리고 트위터에도 제대로 포스트가 잘 이루어진 것을 확인할 수 있습니다.

![파워 앱스 #6][image-36]

<br/>

---

지금까지, [애저 펑션][az func] 앱에 [Open API 익스텐션][gh openapi]을 설치해서 자동으로 Swagger 문서를 만들어주게끔 하는 것과 더불어, 이를 이용해 [파워 오토메이트][power automate]에 쓰이는 [커스텀 커넥터][az cuscon]를 손쉽게 만드는 방법, 그리고 파워 오토메이트에 이 커스텀 커넥터를 쉽게 붙이는 방법, 마지막으로 [파워 앱][power apps]에 파워 오토메이트를 쉽게 연동하는 방법에 대해 알아 보았습니다. 이렇게 애저 펑션에 간단한 익스텐션 하나만 설치하는 것으로 애저 펑션 API의 확장성이 엄청나게 높아지게 되는데, 이를 이용하면 [파워 플랫폼][power platform]에서 필요한 API를 정말 손쉽게 만들 수 있으리라 확신합니다.


## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][az account free]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt msdevkr]


[image-01]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-01.png
[image-02]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-02.png
[image-03]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-03.png
[image-04]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-04.png
[image-05]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-05.png
[image-06]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-06.png
[image-07]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-07.png
[image-08]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-08.png
[image-09]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-09.png
[image-10]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-10.png
[image-11]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-11.png
[image-12]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-12.png
[image-13]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-13.png
[image-14]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-14.png
[image-15]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-15.png
[image-16]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-16.png
[image-17]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-17.png
[image-18]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-18.png
[image-19]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-19.png
[image-20]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-20.png
[image-21]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-21.png
[image-22]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-22.png
[image-23]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-23.png
[image-24]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-24.png
[image-25]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-25.png
[image-26]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-26.png
[image-27]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-27.png
[image-28]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-28.png
[image-29]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-29.png
[image-30]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-30.png
[image-31]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-31.png
[image-32]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-32.png
[image-33]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-33.png
[image-34]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-34.png
[image-35]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-35.png
[image-36]: https://sa0blogs.blob.core.windows.net/aliencube/2020/07/creating-custom-connector-from-azure-functions-with-swagger-36.png

[az account free]: https://azure.microsoft.com/ko-kr/free/?WT.mc_id=dotnet-12565-juyoo&ocid=AID3027813
[ms learn]: https://docs.microsoft.com/ko-kr/learn/?WT.mc_id=dotnet-12565-juyoo&ocid=AID3027813
[yt msdevkr]: https://www.youtube.com/channel/UCdgR-b2t7Byu_UGrHnu-T0g

[gh openapi]: https://github.com/Azure/azure-functions-openapi-extension
[gh openapi docs openapi]: https://github.com/Azure/azure-functions-openapi-extension/blob/main/docs/enable-open-api-endpoints.md
[gh openapi release]: https://github.com/Azure/azure-functions-openapi-extension/releases

[az func]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview?WT.mc_id=dotnet-15268-juyoo&ocid=AID3027813
[az logapp]: https://docs.microsoft.com/ko-kr/azure/logic-apps/logic-apps-overview?WT.mc_id=dotnet-15268-juyoo&ocid=AID3027813
[az apim]: https://docs.microsoft.com/ko-kr/azure/api-management/api-management-key-concepts?WT.mc_id=dotnet-15268-juyoo&ocid=AID3027813
[az cuscon]: https://docs.microsoft.com/ko-kr/connectors/custom-connectors/?WT.mc_id=dotnet-15268-juyoo&ocid=AID3027813

[power platform]: https://powerplatform.microsoft.com/ko-kr/?WT.mc_id=dotnet-15268-juyoo&ocid=AID3027813
[power automate]: https://flow.microsoft.com/ko-kr/?WT.mc_id=dotnet-15268-juyoo&ocid=AID3027813
[power apps]: https://powerapps.microsoft.com/ko-kr/?WT.mc_id=dotnet-15268-juyoo&ocid=AID3027813
