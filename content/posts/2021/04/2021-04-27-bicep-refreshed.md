---
title: "애저 Bicep 되짚어보기"
slug: bicep-refreshed
description: "이 포스트에서는 애저 Bicep 프로젝트 관련 새롭게 추가된 내용에 대해 알아봅니다."
date: "2021-04-27"
image: https://sa0blogs.blob.core.windows.net/aliencube/2021/04/bicep-refreshed-00.png
image_caption: 이두근 표현
author: justin-yoo
category: Azure
tags: azure, bicep, arm, iac
canonical_url: https://blog.aliencube.org/ko/2021/04/21/bicep-refreshed/
featured: false
---


예전에 개인 블로그에 [작성했던 포스트][post 1]에서는 극초기의 [Bicep 프로젝트][bicep]에 대해 소개를 했더랬습니다. 포스팅 당시에는 `0.1.x` 버전이었지만, 지금은 `0.3.x` 버전으로 실제 프로덕션에서 사용할 수 있을 만큼 안정화가 되었습니다. 더불어 다양한 기능 추가도 있었는데요, 이 포스트에서는 신규로 추가된 bicep의 기능에 대해 알아보기로 합니다.


## 애저 CLI 통합 ##

Bicep CLI는 단독으로도 사용할 수 있지만, 만약 [애저 CLI][az cli]를 사용하고 있다면 [v2.20.0 버전][az cli release v2.20.0] 이후부터는 bicep CLI와 통합되었습니다. 따라서 아래와 같은 명령어가 가능합니다.

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=01-bicep-build.sh

> **참고**: `v0.2.x` 버전까지는 여러 bicep 파일을 한번에 컴파일할 수 있었습니다. 하지만, `v0.3.x` 버전부터는 한 번에 bicep 파일 하나만 처리할 수 있도록 바뀌었습니다. 따라서, 한 번에 여러 파일을 빌드하고 싶다면 별도의 작업을 해 주어야 하는데요, 아래는 파워셸을 예로 들어 작성해 본 스크립트입니다.
> 
> https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=02-bicep-build.ps1

이렇게 애저 CLI와 통합된 덕에 bicep 파일을 그대로 애저 CLI를 통해 실행시킬 수도 있습니다. 따라서 아래와 같은 명령어가 가능합니다.

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=03-bicep-deploy.sh


## Bicep 디컴파일 ##

`v0.1.x` 버전에서는 오로지 `.bicep` 파일을 `.json` 파일로 컴파일하는 기능만 가능했습니다. 그런데, [`v0.2.59` 버전][bicep release v0.2.59] 이후로는 기존의 ARM 템플릿을 .bicep 파일로 디컴파일 하는 것도 가능합니다. 이 기능을 이용하면 기존에 사용하던 ARM 템플릿을 유지보수하기 위해 bicep 파일로 변환시키는 데 굉장히 유용하겠죠? 아래와 같은 명령어를 사용하면 됩니다.

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=04-bicep-decompile.sh

> **NOTE**: 이 디컴파일 기능은 ARM 템플릿 안에 `copy` 속성이 있을 경우 아직 제대로 처리하지 못합니다. 좀 더 버전업이 되면 가능해질 것으로 예상하고 있습니다.


## 파라미터 데코레이터 ##

파라미터 작성이 `v0.1.x` 버전에 비해 훨씬 간결해졌습니다. 파라미터의 속성을 데코레이터로 지정할 수 있게끔 개선되었는데요, 아래 코드를 보겠습니다. 저장소 어카운트의 SKU 값은 정해져 있으므로, 아래와 같이 `@allowed` 데코레이터를 사용하면 훨씬 더 가독성이 높아지겠죠?

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=05-bicep-decorators.bicep


## 조건부 리소스 선언 ##

속성값을 지정할 때 삼항연산을 사용하면 조건에 따라 다른 값을 부여할 수 있습니다. 더불어, 아예 `if { ... }` 문을 사용하면, 리소스 자체를 조건부로 선언할 수도 있게끔 개선이 이루어졌습니다. 아래와 같은 형태로 구성해 볼까요? 리소스 그룹의 프로비저닝 지역이 **한국 중부 (Korea Central)**일 때에만 애저 앱 서비스 인스턴스를 프로비저닝하게끔 조건을 걸어 놓은 게 보이죠?

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=06-bicep-conditions.bicep


## 순환문 선언 ##

ARM 템플릿에서는 `copy` 속성과 `copyIndex()` 함수를 이용해서 동일한 리소스를 반복해서 프로비저닝했습니다. bicep 에서는 이를 `for...in` 루프를 사용해서 선언할 수 있습니다. 아래 코드를 볼까요? 애저 앱 서비스 인스턴스를 배열 파라미터를 통해 한 번에 프로비저닝하게끔 선언한 것을 볼 수 있습니다.

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=07-bicep-loops-1.bicep

아래 리소스 선언과 같이 배열과 인덱스를 동시에 활용할 수도 있네요.

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=08-bicep-loops-2.bicep

또한 배열과 상관없이 `range()` 함수를 사용해서 루프를 돌려 리소스를 선언할 수도 있습니다.

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=09-bicep-loops-3.bicep

여기서 한 가지 명심해야 하는 부분이 있습니다. `for...in` 루프를 사용하면 결과적으로 리소스 배열이 생기게 되는데요, 이 `for...in` 루프의 바로 바깥에 반드시 배열 표시(`[...]`)를 해야 합니다. 그러면 나머지는 bicep이 알아서 처리합니다.


## 모듈화 ##

제 최애 파트입니다! ARM 템플릿에서는 [연결 템플릿][az arm template linked] 기능을 이용해 리소스별 모듈화를 시도했다면, bicep에서는 `module` 이라는 키워드를 이용해서 모듈화를 선언합니다. 좀 더 직관적으로 바뀐 셈이죠. 예를 한 번 들어볼까요? 아래와 같이 애저 펑션 인스턴스를 선언하기 위해서는 최소 저장소 어카운트, 컨섬션 플랜, 애저 펑션의 세 가지 리소스가 필요한데, 이를 각각 모듈로 구성해서 하나로 통합하면 됩니다. 물론 개별 모듈은 독립적으로 작동할 수 있어야 합니다.


### 저장소 어카운트 모듈 ###

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=10-bicep-storage-account.bicep


### 컨섬션 플랜 모듈 ###

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=11-bicep-consumption-plan.bicep


### 애저 펑션 모듈 ###

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=12-bicep-function-app.bicep


### 모듈 오케스트레이션 ###

위와 같이 개별 리소스마다 모듈화를 해 놓았다면, 이를 하나로 통합해서 활용할 수 있는 오케스트레이션 파일을 아래와 같이 생성하면 됩니다.

https://gist.github.com/justinyoo/e27d3ddc8d868a1c16293f8286b3ff67?file=13-bicep-azuredeploy.bicep

> **NOTE**: 이 글을 쓰는 시점에서는 ARM 템플릿과 반대로 아직 모듈 참조를 위해 외부 URL을 사용할 수 없습니다.

---

지금까지 애저 bicep의 새로운 기능에 대해 알아 보았습니다. 계속해서 추가 기능이 들어가고 있는 만큼 굉장히 빠르게 업데이트 되고 있으니, 꾸준히 사용해 보면 좋을 것 입니다.


## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][az account free]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt msdevkr]


[az account free]: https://azure.microsoft.com/ko-kr/free/?WT.mc_id=dotnet-25381-juyoo&ocid=AID3027813
[ms learn]: https://docs.microsoft.com/ko-kr/learn/?WT.mc_id=dotnet-25381-juyoo&ocid=AID3027813
[yt msdevkr]: https://www.youtube.com/c/microsoftdeveloperkorea

[post 1]: https://blog.aliencube.org/ko/2020/09/09/bicep-sneak-peek/

[bicep]: https://github.com/Azure/bicep/
[bicep release v0.2.59]: https://github.com/Azure/bicep/releases/tag/v0.2.59

[az cli]: https://docs.microsoft.com/ko-kr/cli/azure/what-is-azure-cli?WT.mc_id=devops-25381-juyoo&ocid=AID3027813
[az cli release v2.20.0]: https://docs.microsoft.com/ko-kr/cli/azure/release-notes-azure-cli?WT.mc_id=devops-25381-juyoo&ocid=AID3027813#march-02-2021

[az arm template linked]: https://docs.microsoft.com/ko-kr/azure/azure-resource-manager/templates/linked-templates?WT.mc_id=devops-25381-juyoo&ocid=AID3027813
