---
title: 공정한 & 편향없는 AI를 위한 Fairlearn 및 애저 MLOps 도구 활용
slug: towards-fairness-ai-with-fairlearn-and-azure-mlops
description: 공정한, 그리고 편향없는 AI를 실현하기 위한 마음가짐/윤리적인 자세도 중요하겠지만, 도구를 잘 활용하여 사용하는 데이터/알고리즘에 대한 편향성을 조기에 발견할 수 있다면 더욱 좋겠죠? Fairlearn 오픈소스와 애저 머신러닝 서비스를 활용한 MLOps 도구를 통해 공정한/편향없는 AI 실현이 어떻게 가능한지 다뤄봅니다.
date: 2021-01-26
image: https://raw.githubusercontent.com/ianychoi/Azure-oss-hands-on-labs/master/06-Azure-ML-with-Fairlearn/_images/fairness_and_interpretability_in_ai.jpg
image_caption: AI에서 공정성 및 해석을 어떻게 해야할까
author: ian-choi
category: Azure
tags: azure, azure-ml, fairness, fairlearn, open-source, mlops, python
canonical_url: https://github.com/ianychoi/Azure-oss-hands-on-labs/blob/master/06-Azure-ML-with-Fairlearn/README.md
featured: false
---

개인의 삶에 영향을 미치는 중요한 결정에 많은 데이터 활용이 이루어지면서 보다 안전하고, 윤리적이면서 책임감있는 사용을 보장하는 AI(인공지능)에 대한 중요성이 대두되고 있습니다. 선한 목적으로 알고리즘을 개발하고 데이터를 활용하더라도 의도하지 않게 성별, 문화적, 민족적 편향성을 AI가 보여준다면 결코 공정하다고 할 수 없을 것입니다.

![애저 머신러닝 - 책임있는 ML](https://raw.githubusercontent.com/ianychoi/Azure-oss-hands-on-labs/master/06-Azure-ML-with-Fairlearn/_images/responsible-ml.png)

마이크로소프트는 책임감있고 사람들의 신뢰를 보장하는 방식으로 AI 시스템이 개발되도록 AETHER 위원회를 비롯한 작업 그룹들이 주도하는 노력과 지원을 계속하고 있습니다 ([참고][ms-ai-approach]). 그 중, 지난 빌드 2020에서는 애저 머신러닝 서비스를 통해 책임있는 ML을 어떻게 구현하는지에 대한 발표가 있었습니다. 데이터 과학자 및 개발자 분들께서 머신러닝 모델을 보다 잘 <b>이해</b>하고, 데이터를 <b>보호</b>하며 머신러닝 전반 과정을 <b>제어</b>하는 과정을 통해 신뢰할 수 있는 AI를 구축가능하다는 것입니다.

![AI에서의 공정성 (Fairness)](https://raw.githubusercontent.com/ianychoi/Azure-oss-hands-on-labs/master/06-Azure-ML-with-Fairlearn/_images/ai-and-fairness-from-build-korea-azure-ai.jpg)

지난 2020년 7월, 마이크로소프트 [Data&AI 클라우드 솔루션 아키텍트이신 박소은님][linkedin-soeun-park]께서 [Azure AI의 새로운 기능][ms-build-korea-2020-azure-track]에 대해 발표하시면서 AI에서의 공정성 (Fairness)을 말씀해주셨습니다. AI 시스템이 공정하지 않게 동작할 때 발생하는 여러 피해 중, 하나는 서비스 품질 (Quality of Service)에 대한 피해가 있습니다. 제품이 특정 그룹에서는 잘 동작하지만 다른 그룹에서는 다른 품질을 보여준다는 것으로, 어떤 음성인식 시스템에서, 남성의 목소리는 잘 인식하는 반면, 여성의 소리를 잘 인식하지 못한다면 서비스 품질 피해가 있다고 할 수 있습니다. 두 번째로는 할당 (Allocation)에 대한 피해로, 예를 들어 대출 심사나 채용 과정에서 관련없는 특징이 고려되어 판단이 되어 의도하지 않은 할당 피해가 있다는 것입니다. 이러한 문제들을 해결하기 위해서는 AI에 접근할 때 윤리적인 마음가짐과 함께 올바른 규정을 준수하는 부분도 중요하겠지만, 어떻게 모델을 분석하고, 특정 그룹의 사람들에게 부정적인 결과를 초래할 수 있는 이러한 행동들이 AI 시스템에 반영되었는지를 잘 확인하는 방법 또한 중요할 것입니다.

도구를 보다 잘 활용하여 사용하는 데이터 및 알고리즘이 공정한지를 어떻게 발견하고 대처할 수 있을까요? 이번 포스트에서는 머신러닝 시스템을 만들 때, 파이썬 라이브러리인 [Fairlearn][fairlearn-website] 오픈소스와 [애저 머신러닝 서비스][ms-azure-ml-docs]를 활용하여 보다 공정하고 편향없는 AI를 어떻게 실현할 수 있는지에 대해 알아보겠습니다.

## Fairlearn 오픈소스를 사용한 머신러닝 모델 평가 ##

Fairlearn은 머신러닝 모델에서 발생할 수 있는 비공정성을 평가하는 기능을 제공하면서 동시에 비공정성을 완화 가능한 알고리즘들을 제공하여 비공정성을 개선하고자 사용하는 오픈소스 툴킷입니다. 파이썬으로 작성된 오픈소스로 [깃허브에 공개][fairlearn-github]되어 있으며, [pip 명령어를 사용][fairlearn-pypi]해 로컬/클라우드 환경에 직접 설치하여 사용하실 수도 있습니다. 본 포스트에서는 비공정성 평가에 대한 부분을 살펴보겠습니다.

https://gist.github.com/ianychoi/3850d7c34c76aa2e6219db698ed57241?file=01-pip-install-fairlearn.sh

일반적으로 머신러닝을 사용해 모델을 정의 후 트레이닝 및 예측하는 과정을 거치며, 머신러닝 모델을 평가하기 위해서는 분류(Classification), 회귀 (Regression), 클러스터링과 같은 모델 유형에 따라 정확도 (Accuracy), 정밀도 (Precision), 재현율 (Recall), AUC, MAE, RMSE 등과 같은 여러 지표 (Metric)들을 사용합니다 ([마이크로소프트 문서][ms-docs-ml-algorithm-evaluation-model-metrics]에서 자세한 내용을 영문으로 확인하실 수 있습니다). Fairlearn에서는 공정성을 평가하기 위해 이와 같은 지표들을 기반으로 1) 민감한 피처를 선택하고 2) 성능과 예측 차이에 따른 평가 결과를 확인하는 라이브러리 함수를 제공합니다. 또한, 대시보드를 Jupyter 노트북에서 확인하여 만든 머신러닝 모델이 공정한지를 보다 쉽게 확인도 가능합니다. 참고로, 애저에서는 뒤에서 설명하는 [애저 머신러닝 서비스에서 Jupyter 노트북을 쉽게 실행][ms-azure-ml-jupyter]할 수 있습니다.

![Fairlearn - 모델 평가](https://raw.githubusercontent.com/ianychoi/Azure-oss-hands-on-labs/master/06-Azure-ML-with-Fairlearn/_images/fairlearn-ml-model-assessment.jpg)

그렇다면, 이제부터는 실제 데이터셋으로 머신러닝 모델을 트레이닝할 때 Fairlearn을 어떻게 사용하는지 살펴보겠습니다. [OpenML에 업로드된 성인 인구조사 데이터셋][openml-adult-census-1590]을 활용해 보겠습니다. [Scikit-learn에서 제공하는 fetch_openml() 함수][scikit-learn-fetch_openml]를 통해 데이터셋 숫자인 1590만 알면 아래 코드 예제와 같이 쉽게 데이터셋을 불러올 수 있습니다. 연간 소득이 5만 달러를 넘는지에 대한 여부를 판단하는 예측 모델을 만들고자 합니다. 여기서 한 가지, 머신러닝 모델 트레이닝을 할 때 성별, 인종과 같은 민감한 피처를 제외하는 것이 좋습니다. 앞에서 언급하였던 할당 피해를 방지할 수 있을 것입니다.

https://gist.github.com/ianychoi/3850d7c34c76aa2e6219db698ed57241?file=02-fetch_openml.py

그리고 머신러닝 모델을 트레이닝할 때, 일반적으로 평가를 위해 데이터를 트레이닝 셋과 테스트 셋으로 분리합니다. 예제에서는 30%를 테스트 셋으로 배정하였으며, 인덱스를 정리하는 짧은 코드를 추가하였습니다.

https://gist.github.com/ianychoi/3850d7c34c76aa2e6219db698ed57241?file=03-dataset-split.py

데이터 준비가 이제 다 끝났으니, 모델 트레이닝 및 예측이 가능하겠죠? 예제에서는 [Scikit-learn에서 제공하는 분류를 위한 의사결정트리인 DecisionTreeClassifier][scikit-learn-tree]를 사용해 보겠습니다. 구체적인 `min_samples_leaf` 및 `max_depth` 값 등을 튜닝하는 것도 중요하겠지만, 본 포스트에서는 AI 공정성을 위한 평가를 어떻게 하는지에 집중하도록 하겠습니다. 테스트셋으로 예측한 결과들을 정확도, 정밀도, 재현율 등 지표를 통해 확인할 수 있으나, 해당 지표 자체만으로 머신러닝 모델이 공정성을 얼마나 확보하였는지를 이해하기에는 쉽지 않은 것 같습니다.

https://gist.github.com/ianychoi/3850d7c34c76aa2e6219db698ed57241?file=04-training.py

Jupyter 노트북에서 Fairlearn 라이브러리에서 제공하는 `FairlearnDashboard()` 함수를 사용하면 대시보드를 불러올 수 있습니다. 함수를 호출할 때, 민감한 피처에 대한 데이터셋과 예측 모델을 지정하면 민감한 피처와 측정하고자 하는 성능 항목을 선택하여 성능과 예측에 대한 차이 (disparity)를 확인하는 대시보드를 제공합니다. 각 민감한 피처 값 유형에 따라 살펴보고자 하는 지표가 과소/과대 예측이 되었는지에 대한 성능 차이 및 데이터 선택에 따른 공정성이 있는지에 대한 예측 차이를 확인할 수 있습니다.

https://gist.github.com/ianychoi/3850d7c34c76aa2e6219db698ed57241?file=05-fairlearn-dashboard.py

## MLOps 실현을 위한 애저 머신러닝 서비스에 모델 & 실험 업로드 ##

방대한 양의 데이터에서부터 머신러닝 모델이 여러 트레이닝 및 예측, 튜닝 과정을 걸쳐 완성되어도, 실제 서비스화를 위해서는 성능 최적화와 함께 트레이닝 데이터 및 모델에 대한 모니터링 및 관리 등 많은 과정이 있습니다. [애저 머신러닝 서비스][ms-azure-ml-service-page]는 사람, 프로세스, 제품이 함께 결합하여 최종 고객에게 가치를 지속적으로 전달한다는 DevOps를 넘어서, 이러한 전반적인 머신러닝을 고려한 MLOps 실천 사례 경험을 반영한 애저 서비스입니다. 복잡한 데이터셋 변화와 발전하는 머신러닝 모델에 대한 추적 뿐만 아니라, 보다 빠르게 머신러닝 모델을 빌드하고 학습, 배포할 수 있는 다양한 생산적인 환경을 개발자와 데이터 과학자에게 제공하고 있습니다. (MLOps에 대한 자세한 내용은 마이크로소프트에서 AI 플랫폼을 담당하시는 [한석진님][linkedin-seokjin-han]께서 공유주신 [MLOps 101 영상][yt-mlops-101-seokjin]을 통해 확인하실 수 있습니다.)

- 실험: 머신러닝을 하는 과정은 1) 데이터셋 준비 2) 모델 트레이닝을 위한 피처 선택 3) 알고리즘 선택 및 파라미터 지정 4) 트레이닝 수행이라는 과정을 거쳐 하나의 머신러닝 모델이 만들어집니다. 이를 애저 머신러닝 서비스에서는 실험이라는 단위로 추상화를 하였습니다. 예를 들어 위 예제에서 테스트셋 비율을 30%로, DecisionTreeClassifier를 사용하여 `min_samples_leaf` 값은 10으로, `max_depth` 값은 4로 지정하였는데, 이 전체를 실행했던 과정을 하나의 실험으로 보는 것입니다. 같은 실험을 반복해서 실행하기도 하며, 데이터셋을 바꿔서 다시 실행하거나, `max_depth` 값을 3으로 바꾸어서 트레이닝을 한다면 또 다른 실험 단위를 실행할 수도 있겠죠.

- 모델: 실험 과정을 거쳐, 반복 & 재사용이 가능한 모델이라는 단위도 생각해볼 수 있습니다. 모델은 1) 어떤 머신러닝 알고리즘을 선택하는지 2) 어떤 파라미터 값을 사용하는지에 대한 내용을 가지는 추상화 단위입니다. 이렇게 모델을 실험과 따로 분리하여 살펴봄으로써, 동일한 모델이 서로 다른 데이터셋을 사용하여 실험할 때 어떤 결과 차이를 보이는지 확인할 수가 있을 것입니다.

무엇보다 애저 머신러닝 서비스는 책임있는 ML을 위한 머신러닝 모델 **이해**, 데이터 **보호**, 전체 과정 **제어**를 위한 쉬운 도구 연계를 염두한 서비스라는 특징을 가지고 있습니다. 위에서 살펴본 Fairlearn의 경우, AI 공정성을 보장하기 위한 머신러닝 모델 이해와 관련이 있는데요, 애저 머신러닝 서비스에서는 Fairlearn 오픈소스와 연동이 가능하여 애저 머신러닝 서비스 UI에서 모델과 실험 단위로 Fairlearn 대시보드를 살펴볼 수 있습니다. 이제부터는 애저 머신러닝 서비스에서 실험과 모델이 어떻게 관리되는지 간단히 살펴보고, 위에서 만든 머신러닝 모델을 애저 머신러닝 서비스에 모델 및 실험 단위로 업로드하여 책임있는 ML을 위해 MLOps를 어떻게 활용할 수 있는지를 살펴보고자 합니다.

업로드를 위해서는 애저 머신러닝 서비스라는 리소스를 만들어야겠죠? 애저 포털에서 애저 머신러닝 서비스를 만드는 방법은 [마이크로소프트 Learn에 설명된 머신러닝 소개 및 작업 영역][ms-learn-azure-ml-workspace] 내용을 참고하실 수 있습니다. 애저 머신러닝 서비스를 쉽게 연결하려면 [azureml-sdk][azureml-sdk-pypi] 및 관련 라이브러리가 필요합니다. 애저 머신러닝 노트북에 대한 한 예제 파일에 나와있는 아래 내용을 통해 의존성있는 라이브러리 목록을 확인할 수 있습니다.

https://gist.github.com/ianychoi/3850d7c34c76aa2e6219db698ed57241?file=05-2-upload-fairness-dashboard.yml

애저 머신러닝 서비스 접속 구성을 담고 있는 `config.json` 파일이 필요합니다. 해당 파일은 애저 머신러닝 서비스 리소스를 애저에서 만든 다음 받으셔서 로컬 및 다른 개발 환경에서 연결하실 수도 있으며, 아니면 애저 머신러닝 스튜디오를 실행한 후, Notebooks 탭에서 Jupyter 노트북을 작성하면 추가한 컴퓨팅 자원에서 바로 `config.json` 파일을 불러오실 수도 있습니다.

![Azure Machine Learning Service - config.json 다운로드](https://raw.githubusercontent.com/ianychoi/Azure-oss-hands-on-labs/master/06-Azure-ML-with-Fairlearn/_images/azure-ml-download-config.png)

https://gist.github.com/ianychoi/3850d7c34c76aa2e6219db698ed57241?file=06-azureml-config.py

그 다음으로, 애저 머신러닝 서비스에 모델을 등록합니다. 이때, 등록할 모델은 이전에 [Scikit-learn에서 제공하는 분류를 위한 의사결정트리인 DecisionTreeClassifier][scikit-learn-tree]를 사용하였고, 구체적인 `min_samples_leaf` 및 `max_depth` 값을 지정하였던, 개체 인스턴스에 대응합니다. 모델을 등록하면 모델 ID 값을 반환하는데, 이 값은 바로 다음에 실험을 추가할 때 사용하므로 변수에 저장해두고자 합니다.

https://gist.github.com/ianychoi/3850d7c34c76aa2e6219db698ed57241?file=07-azure-model-reg.py

애저 머신러닝 서비스에서 Fairlearn에서 실행했던 결과를 포함해 실험으로 관리할 수 있습니다. 이렇게 관리를 하려면 Fairlearn으로 실행한 결과를 계산하여 저장해 두고 있어야겠죠. [fairlearn.metrics][fairlearn-metrics-package] 패키지에 있는 `_create_group_metric_set()` 함수를 통해 미리 계산한 결과를 저장합니다. 

https://gist.github.com/ianychoi/3850d7c34c76aa2e6219db698ed57241?file=08-fairlearn-metric-calc.py

다음으로 애저 머신러닝 서비스에 실험 단위로 업로드합니다. [azureml.core.Experiment 클래스][ms-azureml-core-experiment-class] 인스턴스를 생성한 후, 업로드를 진행합니다. 이 때, 바로 이전에 Fairlearn에서 계산된 결과를 Experiment 인스턴스에 같이 전달합니다.

https://gist.github.com/ianychoi/3850d7c34c76aa2e6219db698ed57241?file=09-azureml-upload.py

이제, 애저 머신러닝 서비스에서 어떻게 보이는지 확인해보면 되겠죠? 애저 머신러닝 스튜디오에서 실험 -> 목록 선택 -> 실행 단위를 선택한 다음, 공정성 탭에 들어가면 확인하실 수 있습니다. 코드 예제와 함께 단계별로 살펴보았는데요, 애저 머신러닝 서비스를 사용한다면 공정성에 대한 부분을 개발자는 Fairlearn으로 관련 지표를 계산하여 업로드하며, 관리자 / 머신러닝 종사자는 애저 머신러닝 스튜디오 UI에서 실험 및 모델 단위로 공정성에 대한 지표를 UI를 통해 쉽게 확인할 수가 있습니다. 또한, 모니터링이나 전체 머신러닝 프로세스를 코드로 잘 관리한다면, 책임있는 ML을 MLOps 실천 사례에 따라 실현할 수 있으며, 구성원 모두 및 외부에도 보다 신뢰있는 AI라고 할 수 있을 것입니다.

![애저 머신러닝 서비스 - 스튜디오에서 공정성 확인](https://raw.githubusercontent.com/ianychoi/Azure-oss-hands-on-labs/master/06-Azure-ML-with-Fairlearn/_images/azure-ml-studio-fairness.png)

이와 같이, 머신러닝 솔루션이 공정하고 예측의 가치를 이해하고 설명하기 쉽도록 하려면 개발자와 데이터 과학자가 AI 시스템의 공정성을 평가하고 관찰된 불공정 문제를 완화하는 데 사용할 수 있는 프로세스를 도구와 함께 구축하는 것이 중요합니다. 2021년 1월 개최된 CES 2021에서 마이크로소프트 최고법률책임자인 브래드 스미스께서는 "기술에는 양심이 없지만 사람에게는 있다"며 "기술이 어떤 방식으로 쓰이든 그것은 사람의 책임"이라고 이야기하였습니다. 이번 포스트에서 소개된 머신러닝 도구 및 실천사례가 잘 활용되어, 보다 공정한, 그리고 편향없는 AI와 함께 모두 지낼 수 있기를 희망합니다.

> 이 포스트에 쓰인 예제 Jupyter 노트북 코드를 [여기](https://github.com/ianychoi/Azure-oss-hands-on-labs/blob/master/06-Azure-ML-with-Fairlearn/fairlearn-quickstart-on-AzureMLStudio.ipynb)에서 확인해 보세요!

그리고 영문으로 되어 있지만, Fairlearn에 대해 자세하게 설명한 많은 내용이 있으며, 해당 내용을 참고하여 본 포스트를 작성하였습니다:
- [Fairlearn - A Python package to assess AI system's fairness][ms-tc-fairlearn]
- [Microsoft Docs: What is responsible machine learning?][ms-docs-responsible-ml]
- [Fairlearn: A toolkit for assessing and improving fairness in AI][ms-research-fairlearn]
- [Creating Fair Machine Learning Models with Fairlearn][towardsdatascience-tutorial-fairness]

## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][ms-az-account-free]
* 애저 머신러닝 서비스에 관심이 있으신가요? ➡️ [애저 머신러닝 서비스 살펴보기][ms-azure-ml-service-page]
* 애저에서 머신러닝을 한다는 것은 무엇일까요? ➡️ [한석진님께서 쓰신 기고를 살펴보세요][seokjin-azure-and-ml]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms-learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt-msdevkr]

[ms-az-account-free]: https://azure.microsoft.com/ko-kr/free/?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-azure-ml-service-page]: https://azure.microsoft.com/ko-kr/services/machine-learning/?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-learn]: https://docs.microsoft.com/ko-kr/learn/?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[yt-msdevkr]: https://www.youtube.com/channel/UCdgR-b2t7Byu_UGrHnu-T0g

[fairlearn-website]: https://fairlearn.github.io
[fairlearn-github]: https://github.com/fairlearn/fairlearn
[fairlearn-pypi]: https://pypi.org/project/fairlearn/
[openml-adult-census-1590]: https://www.openml.org/d/1590
[scikit-learn-fetch_openml]: https://scikit-learn.org/stable/modules/generated/sklearn.datasets.fetch_openml.html
[scikit-learn-tree]: https://scikit-learn.org/stable/modules/tree.html
[scikit-learn-model-evaluation]: https://scikit-learn.org/stable/modules/model_evaluation.html
[yt-mlops-101-seokjin]: https://www.youtube.com/playlist?list=PLDZRZwFT9Wku509LgbJviEcHxX4AYj3QP
[azureml-sdk-pypi]: https://pypi.org/project/azureml-sdk/
[fairlearn-metrics-package]: https://fairlearn.github.io/v0.5.0/api_reference/fairlearn.metrics.html
[towardsdatascience-tutorial-fairness]: https://towardsdatascience.com/a-tutorial-on-fairness-in-machine-learning-3ff8ba1040cb
[seokjin-azure-and-ml]: http://it.chosun.com/site/data/html_dir/2020/08/02/2020080200103.html
[linkedin-soeun-park]: https://www.linkedin.com/in/soeun-park-b55613176/
[linkedin-seokjin-han]: https://www.linkedin.com/in/seokjinhan/

[ms-ai-approach]: https://www.microsoft.com/ko-kr/ai/our-approach?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-build-korea-2020-azure-track]: https://info.microsoft.com/AP-AzureINFRA-WBNR-FY21-07Jul-23-BuildKorea-SRDEM31279_LP02OnDemandRegistration-ForminBody.html?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-docs-ml-algorithm-evaluation-model-metrics]: https://docs.microsoft.com/en-us/azure/machine-learning/algorithm-module-reference/evaluate-model#metrics?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-learn-azure-ml-workspace]: https://docs.microsoft.com/ko-kr/learn/modules/intro-to-azure-machine-learning-service/2-azure-ml-workspace?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-azureml-core-experiment-class]: https://docs.microsoft.com/ko-kr/python/api/azureml-core/azureml.core.experiment(class)?view=azure-ml-py&ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-tc-fairlearn]: https://techcommunity.microsoft.com/t5/educator-developer-blog/fairlearn-a-python-package-to-assess-ai-system-s-fairness/ba-p/1402950?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-docs-responsible-ml]: https://docs.microsoft.com/en-us/azure/machine-learning/concept-responsible-ml?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-research-fairlearn]: https://www.microsoft.com/en-us/research/publication/fairlearn-a-toolkit-for-assessing-and-improving-fairness-in-ai/?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-azure-ml-jupyter]: https://docs.microsoft.com/ko-kr/azure/machine-learning/how-to-run-jupyter-notebooks/?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
[ms-azure-ml-docs]: https://docs.microsoft.com/ko-kr/azure/machine-learning/?ocid=AID3027813&WT.mc_id=aiml-13440-yechoi
