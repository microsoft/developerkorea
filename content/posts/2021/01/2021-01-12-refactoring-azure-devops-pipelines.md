---
title: 애저 DevOps 파이프라인을 리팩토링하는 6가지 방법
slug: 6-ways-refactoring-azure-devops-pipelines
description: 애저 DevOps 파이프라인 YAML 파일을 사용하다 보면 반복적인 작업이 많이 나옵니다. 이 반복적인 작업 부분을 템플릿 형태로 리팩토링할 수 있는 포인트가 최소 여섯 군데 정도인데, 이러한 리팩토링 테크닉에 대해 다뤄봅니다.
date: 2021-01-12
image: https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-00.jpg
image_caption: 퍼즐을 푸는 사람들
author: justin-yoo
category: Azure
tags: devops, azure-devops, azure-pipelines, multi-stage-pipelines, refactoring, yaml
canonical_url: https://blog.aliencube.org/ko/2019/09/04/azure-devops-pipelines-refactoring-technics/
featured: false
---


[애저 DevOps][az devops]에서 [CI/CD 파이프라인][az devops pipelines]을 구성하다보면 보통 반복적인 작업들이 많습니다. 이게 [태스크 Tasks][az devops pipelines tasks] 수준일 수도 있고, [작업 Jobs][az devops pipelines jobs] 수준일 수도 있고, [스테이지 Stages][az devops pipelines stages] 수준일 수도 있는데, 코딩을 할 때는 반복적인 부분을 리팩토링 한다지만, 파이프라인에서 반복적인 부분을 리팩토링할 수는 없을까요? 물론 있습니다. 그것도 파이프라인을 리팩토링할 수 있는 포인트가 최소 여섯 군데 정도 있습니다. 이 포스트에서는 애저 파이프라인의 [YAML][az devops pipelines yaml] 형식 [템플릿][az devops pipelines templates]을 이용해서 반복적으로 나타나는 부분을 리팩토링하는 방법에 대해 알아보겠습니다.

> 이 포스트에 쓰인 예제 파이프라인 코드를 [이 리포지토리](https://github.com/devkimchi/Azure-Pipelines-Template-Sample)에서 확인해 보세요!


## 빌드 파이프라인 ##

우선 일반적인 빌드 파이프라인을 한 번 만들어 보겠습니다. 아래는 그냥 빌드 `Stage`를 작성한 것입니다. `Stages/Stage` 아래 `Jobs/Job` 아래 `Steps/Task`가 들어가 있습니다. `Greeting` 이라는 변수값을 출력시키는 파이프라인입니다 (line #18, 25)

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=pipeline-build-without-template.yaml&highlights=18,25

이 파이프라인을 실행시키면 아래와 같은 결과가 나옵니다. `Hello World`가 보이죠?

![리팩토링 전 애저 빌드 파이프라인 실행 결과](https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-01.png)

이제 이 빌드 파이프라인을 리팩토링할 차례입니다. 리팩토링은 크게 세 곳에서 가능한데요, 하나는 `Steps` 수준, 다른 하나는 `Jobs` 수준, 그리고 마지막 하나는 `Stages` 수준입니다.


## 빌드 파이프라인을 `Steps` 수준에서 리팩토링하기 ##

예를 들어 node.js 기반의 애플리케이션을 하나 만든다고 가정해 보죠. 이 경우 보통 순서가

1. node.js 런타임 설치하기
2. npm 패키지 복원하기
3. 애플리케이션 빌드하기
4. 애플리케이션 테스트하기
5. 아티팩트 생성하기

정도가 될 것입니다. 이 때 마지막 5번 항목을 제외하고는 거의 대부분의 경우 같은 순서로, 그리고 저 1-4번 작업을 한 세트로 해서 진행을 하게 되죠. 그렇다면 이 1-4번 작업 흐름을 그냥 하나로 묶어서 템플릿 형태로 빼 놓을 수도 있지 않을까요? 이럴 때 바로 `Steps` 수준의 리팩토링을 진행하게 됩니다. 만약 다른 작업에서는 이후 추가 작업을 더 필요로 한다고 하면 템플릿을 돌리고 난 후 추가 태스크를 정의하면 되므로 별 문제는 없습니다.

이제 위에 정의한 빌드 파이프라인의 `Steps` 부분을 별도의 템플릿으로 분리합니다. 그렇다면 원래 파이프라인과 템플릿은 아래와 같이 바뀔 것입니다. 원래 파이프라인(`pipeline.yaml`)의 `steps` 항목 아래에 `template` 라는 항목이 생기고 (line #21), `parameters`를 통해 템플릿으로 값을 전달하는 것이 보일 것입니다 (line #22-23).

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=pipeline-build-with-steps-template.yaml&highlights=21-23

그리고 `Steps` 수준 리팩토링 결과 템플릿인 `template-steps-build.yaml`을 보면, 아래와 같이 `parameters`와 `steps`를 정의했습니다 (line #2, 5). 이 `parameters` 항목을 통해 부모 파이프라인과 템플릿 사이 값을 교환할 수 있게 해 줍니다.

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=template-steps-build.yaml&highlights=2,5

이렇게 리팩토링을 한 후 파이프라인을 돌려보면 아래와 같은 결과 화면을 보게 됩니다. 부모 파이프라인에서 템플릿으로 넘겨준 파라미터 값이 잘 표현되는 것이 보이죠?

![Steps 수준 리팩토링 후 애저 빌드 파이프라인 실행 결과](https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-02.png)


## 빌드 파이프라인을 `Jobs` 수준에서 리팩토링하기 ##

이번에는 `Jobs` 수준에서 리팩토링을 한 번 해보겠습니다. 앞서 연습해 봤던 `Steps` 수준 리팩토링은 공통의 태스크들을 묶어주는 정도였다면, `Jobs` 수준의 리팩토링은 그보다 큰 덩어리를 다룹니다. 이 덩어리에는 [빌드 에이전트][az devops pipelines agents]의 종류까지 결정할 수 있고, 템플릿 안의 모든 태스크를 동일하게 가져갈 수 있습니다.

> 물론 [조건 표현식][az devops pipelines conditions]과 같은 고급 기능을 사용하면 좀 더 다양한 시나리오에서 다양한 태스크들을 활용할 수 있습니다.

아래와 같이 부모 파이프라인을 수정해 보죠 (line #13-16).

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=pipeline-build-with-jobs-template.yaml&highlights=13-16

그리고 난 후, 아래와 같이 `template-jobs-build.yaml` 파일을 작성합니다. 파라미터로 `vmImage`와 `message`를 넘겨 템플릿에서 어떻게 사용하는지 살펴보죠 (line #2-4).

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=template-jobs-build.yaml&highlights=2-4

`Jobs` 수준에서 사용하는 빌드 에이전트의 종류까지도 변수화시켜 사용할 수 있는 것이 보이나요? 부모 템플릿에서 에이전트를 `Windows Server 2016` 버전으로 설정했으므로 실제 이를 파이프라인으로 돌려보면 아래와 같은 결과가 나타납니다.

![Jobs 수준 리팩토링후 애저 빌드 파이프라인 실행 결과](https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-03.png)


## 빌드 파이프라인을 `Stages` 수준에서 리팩토링하기 ##

이번에는 `Stages` 수준에서 파이프라인 리팩토링을 시도해 보겠습니다. 하나의 스테이지에는 여러개의 `Job`을 동시에 돌리거나 순차적으로 돌릴 수 있습니다. `Job` 수준에서 돌아가는 공통의 작업들이 있다면 이를 `Job` 수준에서 묶어 리팩토링 할 수 있겠지만, 아예 공통의 `Job`들 까지 묶어서 하나의 `Stage`를 만들고 이를 별도의 템플릿으로 빼낼 수 있는데, 이것이 이 연습의 핵심입니다. 아래 부모 파이프라인 코드를 보세요. `stages` 아래에 곧바로 템플릿을 지정하고 변수를 보냅니다 (line #9-12).

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=pipeline-build-with-stages-template.yaml&highlights=9-12

위에서 언급한 `template-stage-build.yaml` 파일은 아래와 같이 작성할 수 있습니다. 부모에서 받아온 파라미터를 통해 빌드 에이전트에 쓰일 OS와 다른 값들을 설정할 수 있는게 보이죠 (line #2-4)?

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=template-stages-build.yaml&highlights=2-4

이렇게 해서 파이프라인을 실행해 본 결과는 대략 아래와 같습니다. 변수를 통해 전달한 값에 따라 빌드 에이전트가 `Ubuntu 16.04` 버전으로 설정이 되었고, 글로벌 변수 값을 별도로 재정의하지 않았으므로 아래 그림과 같이 `G'day, mate`라는 글로벌 변수 값을 볼 수 있습니다.

![Stages 수준 리팩토링 후 애저 빌드 파이프라인 실행 결과](https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-04.png)


## 빌드 파이프라인을 다단계 템플릿으로 리팩토링하기 ##

이렇게 `Steps` 수준, `Jobs` 수준, `Stages` 수준에서 모두 리팩토링을 해 봤습니다. 그렇다면 리팩토링의 결과물인 템플릿을 다단계로 걸쳐서 사용할 수는 없을까요? 물론 당연히 되죠. 아래와 같이 부모 파이프라인을 수정해 보겠습니다. 이번에는 맥OS를 에이전트로 선택해 볼까요 (line #9-12)?

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=pipeline-build-with-nested-stages-template.yaml&highlights=9-12

`Stage` 수준에서 다단계 템플릿을 만들어서 붙여봤습니다. 이 템플릿 안에서 또다시 `Jobs` 수준의 다단계 템플릿을 호출합니다 (line #11-14).

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=template-stages-nested-build.yaml&highlights=11-14

`Jobs` 수준의 다단계 템플릿은 대략 아래와 같습니다. 그리고, 이 안에서 또다시 앞서 만들어 둔 `Steps` 수준의 템플릿을 호출합니다 (line #17-19).

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=template-jobs-nested-build.yaml&highlights=17-19

이렇게 다단계로 템플릿을 만들어 붙여놓은 후 파이프라인을 돌려보면 아래와 같습니다.

![다단계 리팩토링 후 애저 빌드 파이프라인 실행 결과](https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-05.png)

아주 문제 없이 다단계 템플릿이 잘 돌아가는게 보이죠?

지금까지 빌드 파이프라인을 리팩토링해 봤습니다. 이제 릴리즈 파이프라인으로 들어가 보겠습니다.


## 릴리즈 파이프라인 ##

릴리즈 파이프라인은 빌드 파이프라인과 크게 다르지 않습니다. 다만 `job` 대신 [`deployment job`][az devpos pipelines deploymentjobs]을 사용한다는 차이가 있을 뿐입니다. 이 둘의 차이에 대해 얘기하는 것은 이 포스트의 범위를 벗어나니 여기까지만 하기로 하고, 실제 릴리즈 파이프라인의 구성을 보겠습니다. 템플릿 리팩토링 없는 전형적인 릴리즈 스테이지는 아래와 같습니다.

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=pipeline-release-without-template.yaml&highlights=9

위 코드를 보면 `Jobs` 수준에 `deployment`를 사용해서 작업 단위를 정의한 것을 볼 수 있죠 (line #9)? 이를 실행시킨 결과는 대략 아래와 같습니다.

![리팩토링 전 애저 릴리즈 파이프라인 실행 결과](https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-06.png)

이제 이 릴리즈 파이프라인을 동일하게 세 곳, `Steps`, `Jobs`, `Stages` 수준에서 리팩토링을 할 수 있습니다. 각각의 리팩토링 방식은 크게 다르지 않으므로 아래 리팩토링 결과만을 적어놓도록 하겠습니다.


## 릴리즈 파이프라인을 `Steps` 수준에서 리팩토링하기 ##

우선 `Steps` 수준에서 릴리즈 템플릿을 만들어 보도록 하죠. 부모 템플릿은 아래와 같습니다 (line #24-26).

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=pipeline-release-with-steps-template.yaml&highlights=24-26

그리고 템플릿으로 빼낸 `Steps`는 아래와 같습니다. 앞서 빌드 파이프라인에서 사용한 템플릿과 구조가 다르지 않죠 (line #2-3)?

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=template-steps-release.yaml&highlights=2-3

그리고 그 결과를 보면 아래와 같습니다.

![Steps 수준 리팩토링 후 애저 릴리즈 파이프라인 실행 결과](https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-07.png)


## 릴리즈 파이프라인을 `Jobs` 수준에서 리팩토링하기 ##

이번에는 릴리즈 파이프라인을 `Jobs` 수준에서 리팩토링해 보겠습니다 (line #13-17).

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=pipeline-release-with-jobs-template.yaml&highlights=13-17

그리고 리팩토링한 템플릿은 아래와 같습니다. 여기서 눈여겨 봐야 할 부분은 바로 [`environment`][az devops pipelines environments] 이름도 파라미터로 처리가 가능하다는 점입니다 (line #14). 즉, 거의 대부분의 설정을 부모 파이프라인에서 파라미터로 내려주면 템플릿에서 받아 처리가 가능합니다 (line #2-5).

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=template-jobs-release.yaml&highlights=2-5,14

![Jobs 수준 리팩토링 후 애저 릴리즈 파이프라인 실행 결과](https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-08.png)


## 릴리즈 파이프라인을 `Stages` 수준에서 리팩토링하기 ##

더 이상의 자세한 설명은 생략합니다 (line #5-9). 😉

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=pipeline-release-with-stages-template.yaml&highlights=5-9

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=tmplate-stages-release.yaml&highlights=2-5

![Stages 수준 리팩토링 후 애저 릴리즈 파이프라인 실행 결과](https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-09.png)


## 릴리즈 파이프라인을 다단계 템플릿으로 리팩토링하기 ##

릴리즈 파이프라인 역시 다단계 템플릿으로 구성이 가능합니다.

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=pipeline-release-with-nested-stages-template.yaml&highlights=5-9

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=template-stages-nested-release.yaml&highlights=2-5,12-16

https://gist.github.com/justinyoo/41e3c56debe1eaa0bb1d0ac062bb38b9?file=template-jobs-nested-release.yaml&highlights=20-22

![다단계 리팩토링 후 애저 릴리즈 파이프라인 실행 결과](https://sa0blogs.blob.core.windows.net/aliencube/2019/08/azure-devops-pipelines-refactoring-technics-10.png)

---

이렇게 빌드 및 릴리즈 파이프라인을 모든 [`Stages`][az devops pipelines stages], [`Jobs`][az devops pipelines jobs], [`Steps`][az devops pipelines tasks] 수준에서 [템플릿][az devops pipelines templates]을 이용해 리팩토링을 해 보았습니다. 파이프라인 작업을 하다 보면 분명히 리팩토링이 필요한 순간이 생깁니다. 그리고 어느 수준에서 템플릿을 만들어 써야 할 지는 전적으로 상황마다 다르다고 할 수 있습니다.

다만 한 가지 고려해야 할 것은, 템플릿은 가급적이면 단순한 작업을 할 수 있게끔 만드는 것이 좋습니다. 템플릿 표현식을 보면 조건문도 있고 반복문도 있고 굉장히 고급 기능을 사용할 수 있긴 하지만, 우선은 단순하게 시작해서 템플릿을 다듬어 나가는 것이 좋을 것입니다. 아무쪼록 [애저 데브옵스 파이프라인][az devops pipelines]의 [다중 스테이지 파이프라인 기법][az devops pipelines multi-stage]을 통해 다양한 템플릿 활용 테크닉을 도입해 보고 그 강력함을 느낄 수 있기를 바랍니다.


## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][az account free]
* 애저 DevOps에 관심이 있으신가요? ➡️ [무료 애저 DevOps 사용하기][az devops free]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt msdevkr]


[az account free]: https://azure.microsoft.com/ko-kr/free/?WT.mc_id=devops-12575-juyoo
[az devops free]: https://azure.microsoft.com/ko-kr/services/devops/?WT.mc_id=devops-12575-juyoo
[ms learn]: https://docs.microsoft.com/ko-kr/learn/?WT.mc_id=devops-12575-juyoo
[yt msdevkr]: https://www.youtube.com/channel/UCdgR-b2t7Byu_UGrHnu-T0g

[az devops]: https://docs.microsoft.com/ko-kr/azure/devops/user-guide/what-is-azure-devops?view=azure-devops&WT.mc_id=devops-12575-juyoo
[az devops pipelines]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/get-started/what-is-azure-pipelines?view=azure-devops&WT.mc_id=devops-12575-juyoo
[az devops pipelines tasks]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/process/tasks?view=azure-devops&tabs=yaml&WT.mc_id=devops-12575-juyoo
[az devops pipelines jobs]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/process/phases?view=azure-devops&tabs=yaml&WT.mc_id=devops-12575-juyoo
[az devpos pipelines deploymentjobs]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/process/deployment-jobs?view=azure-devops&WT.mc_id=devops-12575-juyoo
[az devops pipelines stages]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/process/stages?view=azure-devops&tabs=yaml&WT.mc_id=devops-12575-juyoo
[az devops pipelines yaml]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/yaml-schema?view=azure-devops&tabs=schema%2Cparameter-schema&WT.mc_id=devops-12575-juyoo
[az devops pipelines templates]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/process/templates?view=azure-devops&WT.mc_id=devops-12575-juyoo
[az devops pipelines conditions]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/process/conditions?view=azure-devops&tabs=yaml&WT.mc_id=devops-12575-juyoo
[az devops pipelines agents]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/agents/agents?view=azure-devops&tabs=browser&WT.mc_id=devops-12575-juyoo
[az devops pipelines multi-stage]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/get-started/multi-stage-pipelines-experience?view=azure-devops&WT.mc_id=devops-12575-juyoo
[az devops pipelines environments]: https://docs.microsoft.com/ko-kr/azure/devops/pipelines/process/environments?view=azure-devops&WT.mc_id=devops-12575-juyoo
