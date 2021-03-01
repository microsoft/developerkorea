---
title: "애저 키 저장소 시크릿 로테이션 관리하기"
slug: keyvault-secrets-rotation-management-in-bulk
description: "이 포스트에서는 애저 키 저장소의 시크릿 값을 로테이션할 때 애저 펑션을 이용해 일정 기간 이상 오래된 시크릿 값을 한 번에 비활성화 시키는 방법에 대해 알아봅니다."
date: 2021-02-23
image: https://sa0blogs.blob.core.windows.net/aliencube/2021/02/keyvault-secrets-rotation-management-00.png
image_caption: 애저 키 저장소 시크릿 관리하기
author: justin-yoo
category: Azure
tags: azure-keyvault, azure-functions, azure-sdk, secret-management
canonical_url: https://blog.aliencube.org/ko/2021/02/17/keyvault-secrets-rotation-management/
featured: false
---


얼마전 [애저 키 저장소][az kv] 시크릿 값을 [애저 앱 서비스][az appsvc] 혹은 [애저 펑션][az fncapp]에서 참조할 때, 더이상 버전을 명시하지 않아도 된다는 [공지][az kv announcement]가 있었습니다. 따라서, [지난 포스트][post prev]에서 언급했던 [애저 키 저장소의 시크릿 값][az kv secrets]을 참조하는 방법들 중 두번째 방법이 이전에는 덜 효율적이었다면 이제는 가장 효율적인 접근 방식이 되었습니다.

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=01-keyvault-reference.txt

위와 같이 설정하면 애저 앱 서비스와 애저 펑션 앱에서 가장 최신 버전의 시크릿 값을 자동으로 가져와서 보여줍니다. 만약 최신 버전의 시크릿 값이 생성된지 아직 만 하루가 지나지 않았다면, 애저 앱 서비스 혹은 애저 펑션 내부적으로 작동하는 캐싱 메카니즘이 완전히 값을 받아오지 않았을 수도 있기 때문에, 이전 버전과 함께 [로테이션][az kv secrets rotation]을 시켜줘야 합니다. 이 때 로테이션을 위해서는 가급적이면 두 가지 버전 정도로만 활성화 상태로 유지하고 나머지는 비활성화 시켜주는 것이 보안상의 관점에서도 좋습니다.

* ***애저 키 저장소 시크릿 로테이션 관리***
* [이벤트 기반 애저 키 저장소 시크릿 로테이션 관리][post next]

[애저 키 저장소][az kv]에 저장할 수 있는 시크릿의 갯수는 딱히 제한된 것이 없습니다. 따라서 현업에서 사용하다 보면 굉장히 많은 수의 시크릿을 저장하게 되는데, 이럴 경우 로테이션에 더이상 쓰이지 않는 시크릿 버전을 일일이 찾아 비활성화 시켜주기에는 너무 많을 수 있습니다. 그렇다면, 이를 자동화할 수 있는 방법에는 무엇이 있을까요? 이 포스트에서는 오래되었지만 여전히 활성화 상태로 남아있는 시크릿 버전들을 일괄적으로 비활성화시키는 방법을 애저 펑션으로 구현해 보기로 합니다.

> 실제 작동하는 코드를 보고 싶으신가요? 이 [깃헙 리포지토리][gh sample]에서 다운로드 받아 로컬에서 돌려보세요!


## 애저 키 저장소 SDK ##

애저 키 저장소를 다루는 SDK는 현재 두 가지 버전이 있습니다.

* [Microsoft.Azure.KeyVault][nuget sdk kv old]
* [Azure.Security.KeyVault.Secrets][nuget sdk kv new]

이 중 전자는 이제 deprecated 된 버전이라서, 후자를 사용하면 됩니다. 이와 더불어 [Azure.Identity][nuget sdk identity] SDK도 함께 다운로드 받아 사용하도록 하겠습니다. 애저 펑션 프로젝트를 생성한 후 아래와 같이 두 NuGet 패키지를 설치합니다.

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=02-add-nuget-packages.sh

또한, 키 저장소 SDK 패키지는 `IAsyncEnumerable` 인터페이스를 사용하므로 [System.Linq.Async][nuget linq async] 패키지도 함께 다운로드 받습니다.

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=03-add-nuget-packages.sh

> **NOTE**: 애저 펑션은 아직 .NET 5를 지원하지 않으므로 `System.Linq.Async` 5.0.0 버전의 패키지를 설치하지 않도록 조심합니다.

이제 필요한 라이브러리 설치는 다 끝났고, 실제로 펑션 코드를 구현해 볼까요?


## 오래된 시크릿 버전 비활성화를 위한 애저 펑션 구현 ##

아래 명령어를 통해 애저 펑션 [HTTP 트리거][az fncapp trigger http]를 하나 만들겠습니다.

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=04-add-new-httptrigger.sh

기본 HTTP 트리거 템플릿으로 펑션이 하나 만들어 졌습니다. 이제 이 펑션 메소드의 `HttpTrigger` 바인딩을 아래와 같이 바꿔보겠습니다. HTTP 메소드는 `POST` 하나로 한정하고, 라우팅 URL을 `secrets/all/disable`로 두었습니다 (line #5).

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=05-secrets-httptrigger-01.cs&highlights=5

환경 변수를 통해 아래 두 값을 받아옵니다. 하나는 애저 키 저장소에 접근할 수 있는 URI이고, 다른 하나는 애저 키 저장소 인스턴스를 호스팅하는 테넌트의 ID값입니다.

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=05-secrets-httptrigger-02.cs

다음으로는 애저 키 저장소에 접근할 수 있는 `SecretClient` 인스턴스를 생성합니다. 이 때 인증 옵션을 `DefaultAzureCredentialOptions` 인스턴스를 통해 제공해야 하는데요, 만약 개발하려는 로컬 컴퓨터에서 애저에 로그인한 계정이 여러 개의 테넌트 정보를 갖고 있다면, 아래와 같이 명시적으로 테넌트 ID 값을 지정해 줘야 합니다. 그렇지 않으면 [인증 에러][nuget sdk identity error]가 발생합니다 (line #4-6).

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=05-secrets-httptrigger-03.cs&highlights=4-6

이제 모든 시크릿을 가져와서 하나씩 처리를 해야 합니다. 가장 먼저 할 일은 모든 시크릿을 가져오는 것입니다 (line #2-4).

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=05-secrets-httptrigger-04.cs&highlights=2-4

이제 각각의 시크릿을 하나씩 돌면서 모든 버전을 가져옵니다. 단, 활성화 된 것만 가져오면 되므로 아래와 같이 `WhereAwait` 구문으로 필터링을 합니다 (line #7). 또한 `OrderByDescendingAwait` 구문을 이용해 시간의 역순으로 정렬해서 가장 최근 것이 맨 앞으로 오게끔 합니다 (line #8).

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=05-secrets-httptrigger-05.cs&highlights=7,8

만약 해당 시크릿에는 활성화된 버전이 없다면, 더이상 처리할 것이 없으므로 넘어갑니다.

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=05-secrets-httptrigger-06.cs

만약 해당 시크릿에는 활성화된 버전이 하나뿐이라면, 더이상 처리할 것이 없으므로 넘어갑니다.

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=05-secrets-httptrigger-07.cs

만약 해당 시크릿의 최신 버전이 생성된지 만 하루가 안 됐다면, 아직 로테이션이 필요하므로 넘어갑니다.

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=05-secrets-httptrigger-08.cs

이제 남은 시크릿 버전을 대상으로 비활성화 처리를 해야 합니다. 가장 최신의 버전은 건너뛰고 그 다음부터 처리합니다 (line #2). 그리고 `Enabled` 값을 `false`로 변경하고 (line #6), 업데이트합니다 (line #8).

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=05-secrets-httptrigger-09.cs&highlights=2,6,8

마지막으로 처리 결과를 저장한 변수를 응답 개체에 실어 반환합니다.

https://gist.github.com/justinyoo/75c16e773d9e1c8b8a1d5d5efa37f9c9?file=05-secrets-httptrigger-10.cs

이렇게 한 후 실제로 애저 펑션을 실행시켜 보면 가장 최신의 시크릿 버전을 제외한 모든 오래된 버전이 비활성화 된 것을 확인할 수 있는데요, 이 펑션앱에서 [HTTP 트리거][az fncapp trigger http] 대신 [타이머 트리거][az fncapp trigger timer]를 붙인다든가, 아니면 [애저 로직 앱][az logapp]을 연동시켜 스케줄링을 걸어 놓는다면 더이상 활성화 되어 있지만 더이상 사용하지 않는 애저 키 저장소의 시크릿 버전들에 대한 걱정을 덜 수 있을 것입니다.

---

지금까지 [애저 키 저장소][az kv]의 시크릿 값을 [애저 앱 서비스][az appsvc] 혹은 [애저 펑션][az fncapp]에서 참조할 때 더이상 사용하지 않는 시크릿 버전을 자동으로 비활성화 시키는 방법에 대해 알아 보았습니다. 이렇게 자동화를 시켜놓으면 추가적인 관리 부담을 줄일 수 있으니 한 번 시도해 보면 좋겠습니다. [다음 포스트][post next]에서는 시크릿에 새 버전이 추가될 경우 발생하는 이벤트를 통해 특정 시크릿만을 대상으로 로테이션 관리를 하는 방법에 대해 알아보겠습니다.


## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][az account free]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt msdevkr]


[post prev]: https://blog.aliencube.org/ko/2020/04/30/3-ways-referencing-azure-key-vault-from-azure-functions/
[post next]: /developerkorea/posts/2021/03/02/event-driven-keyvault-secrets-rotation-management/

[az account free]: https://azure.microsoft.com/ko-kr/free/?WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813
[ms learn]: https://docs.microsoft.com/ko-kr/learn/?WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813
[yt msdevkr]: https://www.youtube.com/channel/UCdgR-b2t7Byu_UGrHnu-T0g

[gh sample]: https://github.com/devkimchi/KeyVault-Reference-Sample/tree/2021-02-17

[az logapp]: https://docs.microsoft.com/ko-kr/azure/logic-apps/logic-apps-overview?WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813

[az appsvc]: https://docs.microsoft.com/ko-kr/azure/app-service/?WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813

[az fncapp]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview?WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813
[az fncapp trigger http]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=csharp&WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813
[az fncapp trigger timer]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-bindings-timer?tabs=csharp&WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813

[az kv]: https://docs.microsoft.com/ko-kr/azure/key-vault/general/overview?WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813
[az kv announcement]: https://azure.microsoft.com/ko-kr/updates/versions-no-longer-required-for-key-vault-references-in-app-service-and-azure-functions/?WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813
[az kv secrets]: https://docs.microsoft.com/ko-kr/azure/key-vault/secrets/about-secrets?WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813
[az kv secrets rotation]: https://docs.microsoft.com/ko-kr/azure/app-service/app-service-key-vault-references?WT.mc_id=dotnet-16807-juyoo&ocid=AID3027813#rotation

[nuget sdk kv old]: https://www.nuget.org/packages/Microsoft.Azure.KeyVault/
[nuget sdk kv new]: https://www.nuget.org/packages/Azure.Security.KeyVault.Secrets/
[nuget linq async]: https://www.nuget.org/packages/System.Linq.Async/
[nuget sdk identity]: https://www.nuget.org/packages/Azure.Identity/
[nuget sdk identity error]: https://github.com/Azure/azure-sdk-for-net/issues/11559#issuecomment-620233531
