---
title: "WebSub에서 CloudEvents를 통해 이벤트그리드로 전환하고 다양한 소셜 미디어로 알리기"
slug: websub-to-eventgrid-via-cloudevents-and-beyond
description: "이 포스트는 유튜브 채널에 새 비디오가 올라왔을 때, 이 이벤트를 CloudEvents 형식으로 변환해서 애저 이벤트그리드로 발행한 후, 로직 앱을 이용해 다양한 소셜 미디어로 확산시키는 일련의 워크플로우에 대해 다룹니다."
date: 2021-02-02
image: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-00-ko.png
image_caption: 유튜브 WebSub에서 CloudEvents를 거쳐 이벤트그리드로 변환하기
author: justin-yoo
category: Azure
tags: azure-eventgrid, azure-logicapps, azure-functions, cloudevents
canonical_url: https://blog.aliencube.org/ko/2021/01/27/websub-to-eventgrid-via-cloudevents-and-beyond/
featured: false
---


유튜브 채널을 하나 운영하고 있다고 가정하겠습니다. 새 비디오가 하나 업로드 되었을 때, 이를 내가 운영하는 다른 소셜 미디어에 함께 노출 시키고 싶다면 어떻게 하면 좋을까요? 이미 시장에는 이를 위한 다양한 유료 도구들이 많이 나와 있고, 소셜 미디어 마케팅을 전문으로 하는 회사도 많습니다. 게다가 그런 회사의 경우는 자체적인 솔루션도 갖고 있으니, 이를 이용하면 쉽겠죠. 그런데, 만약 여러 가지 이유로 내가 직접 이런 도구들을 만들어서 사용하고 싶다면 어떨까요? 기존의 제품/서비스들이 제공하는 기능이 내 용도와는 다르다면 어떻게 할까요? 이럴 경우에는 한 번 직접 만들어 보는 것도 좋습니다.

이 포스트에서는 유튜브에 새 비디오가 올라올 때부터 다른 소셜 미디어에 노출 시킬 때 까지의 전체적인 워크플로우를 [애저 이벤트그리드][az evtgrd], [애저 펑션][az fncapp], [애저 로직 앱][az logapp] 등의 다양한 애저 서버리스 서비스를 통해 구현해 보기로 하겠습니다.

> 구현한 솔루션의 소스 코드는 이곳 [깃헙 리포지토리][gh sample]에서 다운로드 받아 보세요!


## 유튜브 알림 구독 ##

유튜브에서는 [웹훅 알림][yt webhook]을 위해 [PubSubHubbub][pshb]이라는 규약을 사용합니다. 이 규약은 현재 [WebSub][websub]이라는 이름으로 [2016년에 최초 가안][websub wd 1]이 나온 이후 2018년에 웹표준으로 지정됐죠.

유튜브의 모든 채널은 이미 구글이 운영하는 [WebSub 허브][yt websub hub]에 등록이 되어 있으므로, 특정 채널에 대한 비디오 업데이트 알림을 받기 위해서는 이 허브에 [구독 신청][yt websub sub]만 하면 됩니다. 아래 그림과 같이 메시지 처리기(Message Handler) URL을 넣고, 유튜브 채널 URL을 입력한 후 `Do It!` 버튼을 클릭하면 등록이 끝납니다.

![유튜브 WebSub 구독하기][image-01]

단, 여기서 주의해야 할 점이 하나 있습니다. 메시지 처리기 URL로 호출하는 애플리케이션은 구독 등록이 끝남과 동시에 [유효성 검증][websub verification]을 위한 API 호출을 받게 되는데, 이를 통과해야만 구독 절차가 완전히 끝나게 됩니다. 이 유효성 검증을 통과하지 못하면 아무리 구독을 해도 알림을 받을 수 없습니다.


## WebSub 구독 요청 검증 ##

WebSub 구독 요청에 대한 검증을 위해서 메시지 처리기 URL은 검증 요청이 들어왔을 때 아래와 같은 내용을 처리해 줘야 합니다.

* 검증 요청은 `GET` 메소드로 아래와 같은 쿼리스트링 파라미터를 전송하는데요,
  * `hub.mode`: `subscribe` 문자열
  * `hub.topic`: 구독하고자 하는 유튜브 채널 URL
  * `hub.challenge`: WebSub 허브에서 생성한 임의의 문자열로 구독 요청에 대한 검증에 사용합니다
  * `hub.lease_seconds`: 구독 요청 유효 기간으로 이 기간 안에 구독 요청에 대한 검증을 통과하지 못하면 이 요청은 자동으로 폐기됩니다
* 검증 요청에 대한 응답으로 응답 개체 본문에 `hub.challenge` 값만 추가해서 200 응답 코드와 함께 반환합니다
  * 응답 개체 본문에 `hub.challenge` 이외의 다른 값이 들어가면 이 응답은 WebSub 허브에서 유효한 응답으로 처리하지 않습니다

이 구독 요청 검증 로직을 [애저 펑션][az fncapp]으로 구현해 보면 대략 아래와 같습니다.

https://gist.github.com/justinyoo/cb04844306f9a44dcdcaaa70a6a55326?file=01-callback-1.cs

위와 같이 메시지 처리를 위한 구독 요청 검증에 성공했다면, WebSub 허브는 앞으로 계속해서 유튜브 채널에 새 비디오가 올라올 때마다 알림 이벤트를 메시지 처리기 쪽으로 보내게 되죠.


## WebSub 알림 피드 변환 ##

WebSub 역시도 어디까지나 [발행자/구독자(Publisher/Subscriber; Pub/Sub) 패턴][eip pubsub]을 따르기 때문에 크게 새로울 것은 없습니다. 다만, WebSub으로 주고 받는 데이터는 ATOM 피드 형식을 따르기 때문에 구독자가 ATOM 피드 형식의 XML 문서를 해석해서 처리할 수만 있으면 됩니다. 그런데, 이벤트 구독자 쪽에 ATOM 피트 형태의 XML 데이터를 강제하는 것은 이벤트 발행자와 구독자 사이에 강한 커플링을 유도합니다. 구독자가 어떤 식으로 데이터를 처리할 지 알 수 없는 상황에서 이를 강제하는 것은 바람직하지 않기 때문에, 중간에 표준 데이터 형식 혹은 캐노니컬 데이터 형식으로 바꿔주는 것이 좋습니다.

따라서, 여기서는 [CloudEvents][ce] 형식을 이용해서 캐노니컬 데이터 형식으로 변환합니다. 이 포스트에서는 이 캐노니컬 데이터 변환 과정을 두 단계로 나눴는데, 하나씩 설명해 보기로 하겠습니다.


### 1. WebSub 알림 피드 ➡️ CloudEvents 형식 변환 ##

이 첫번째 단계의 목적은 WebSub에 대한 의존성을 끊어내는 데 있습니다. 따라서, WebSub에서 전달된 ATOM 피드의 XML 데이터를 별다른 변환 없이 그대로 CloudEvents 형식에 담습니다. 유튜브에 새 비디오가 올라왔을 때 WebSub을 통해 받는 알림 피드의 데이터는 대략 아래와 비슷하게 생겼습니다.

https://gist.github.com/justinyoo/cb04844306f9a44dcdcaaa70a6a55326?file=02-websub-feed.xml

이 요청 데이터를 아래와 같이 단순 문자열로 받아 냅니다.

https://gist.github.com/justinyoo/cb04844306f9a44dcdcaaa70a6a55326?file=03-callback-2.cs

이와 더불어 알림 피드 요청 헤더는 아래와 같은 값을 포함하고 있는데요,

https://gist.github.com/justinyoo/cb04844306f9a44dcdcaaa70a6a55326?file=04-websub-request-header.txt

이 헤더 값을 아래와 같이 추출해 냅니다.

https://gist.github.com/justinyoo/cb04844306f9a44dcdcaaa70a6a55326?file=05-callback-3.cs

그리고 아래와 같이 이벤트 타입과 이벤트 데이터 타입을 설정합니다.

https://gist.github.com/justinyoo/cb04844306f9a44dcdcaaa70a6a55326?file=06-callback-4.cs

[지난 포스트][post prev 1]에서 언급한 바와 같이 현재 [애저 펑션][az fncapp]의 [이벤트그리드 바인딩][az fncapp binding evtgrd]은 [CloudEvents 형식을 아직 지원하지 않기 때문에][az evtgrd ce], 아래와 같이 펑션 코드 안에서 직접 처리를 해 줘야 합니다.

https://gist.github.com/justinyoo/cb04844306f9a44dcdcaaa70a6a55326?file=07-callback-5.cs

여기까지 해서, WebSub에서 받아온 이벤트 데이터를 그대로 CloudEvents를 이용한 캐노니컬 형식으로 변환해서 애저 이벤트그리드로 다시 보냅니다. 이렇게 보내진 CloudEvents 형식 데이터는 아래와 같습니다.

https://gist.github.com/justinyoo/cb04844306f9a44dcdcaaa70a6a55326?file=08-cloudevents-from-websub.json

여기까지 해서 아래와 같은 변환 절차가 끝났습니다.

![유튜브 WebSub에서 애저 이벤트그리드로 변환하기][image-02]

이제 다음 단계로 넘어가도록 하지요.


### 2. WebSub XML 데이터 가공 ##

두 번째 단계에서는 WebSub XML 데이터를 소셜 미디어 확산을 위해 필요한 데이터 형태로 가공하는 일을 합니다.

앞서 받아온 WebSub 데이터에는 비디오 ID, 채널 ID 등과 같은 제한적인 정보만 들어있습니다. 따라서, 이를 이용해서 구체적인 정보를 YouTube API를 통해 받아와야 다음 단계의 소셜 미디어 노출에 필요한 데이터를 가공할 수 있습니다. [애저 이벤트그리드][az evtgrd]에 발행된 이벤트 데이터를 받아 처리하려면 우선 이벤트 처리기를 등록해야 합니다. 이 등록 과정도 앞서 WebSub 구독 등록 절차와 같이 [유효성 검사][az evtgrd delivery auth]가 필요합니다. 그런데, 이벤트 처리기로 [애저 로직 앱][az logapp]을 선택하면 이 유효성 검사 부분을 내부적으로 처리를 해주기 때문에 여기서는 로직 앱을 이용하면 편리합니다.

로직 앱 이벤트 처리기에서 처음 하는 일은 이벤트그리드에서 받아온 이벤트 데이터가 내가 필요로 하는 데이터인지를 확인하는 것입니다. 내가 필요로 하는 이벤트 데이터라면 채널 정보와 이벤트 타입이 맞아야 합니다. 내가 필요로 하는 데이터가 아니라면 더이상 로직 앱 워크플로우를 실행하지 않고 멈추게 됩니다.

![이벤트 데이터 확인하기][image-03]

내가 처리하고자 하는 이벤트 데이터라면 이를 애저 펑션으로 보내 데이터를 가공합니다.

![이벤트 데이터 가공하기][image-04]

애저 펑션에서는 유튜브 API를 이용해서 비디오의 구체적인 정보를 받아와 가공한 후 다시 로직 앱으로 데이터를 반환합니다. 애저 펑션이 반환하는 데이터는 대략 아래와 같이 생겼습니다.

https://gist.github.com/justinyoo/cb04844306f9a44dcdcaaa70a6a55326?file=09-video-details.json

여기까지 해서 대략 이벤트 데이터를 가공하는 작업이 끝났습니다.

![이벤트 데이터 가공하기 다이어그램][image-05]


## 소셜 미디어 노출 ##

이 로직 앱이 해야 하는 나머지 일은 다른 소셜 미디어 확산 도구들이 이 데이터를 받아 처리할 수 있게끔 해 주는 것인데요, 여기에는 두 가지 방법이 있습니다.

* 이 로직 앱에 직접 다른 소셜 미디어 노출을 위한 API를 연결하는 것
* 소셜 미디어에 노출시키기 위한 도구들이 이용할 수 있게끔 이벤트를 던지는 것

첫 번째 방법은 의존성이 생깁니다. 만약, 새로운 도구가 추가된다거나, 기존 도구를 제거한다거나 하면 이 로직 앱을 반드시 수정해야 합니다. 이는 유지보수 측면에서는 그다지 좋은 방법은 아니죠. 반면에 두 번째 방법은 다시 이벤트를 생성해서 뿌리면, 다른 소셜 미디어 확산 도구들이 각자 알아서 이 이벤트를 받아 처리하면 되기 때문에 의존성을 없앨 수 있습니다. 복잡도가 한 단계 증가하기는 하지만, 유지보수 측면에서는 굉장히 편리합니다. 따라서, 여기서는 두 번째 방법을 선택했습니다.


### 1. 이벤트그리드로 가공된 데이터 발행하기 ###

이벤트그리드로 다시 이벤트 데이터를 발행하기 위해서는 우선 CloudEvents 형식으로 데이터를 구성해야 합니다. 앞서 받아온 데이터는 말 그대로 데이터를 가공만 한 것이고, 아래 로직 앱 액션에서 CloudEvents 형식으로 데이터를 가공합니다.

![CloudEvents 형식으로 변환하기][image-06]

마지막으로 이 이벤트 데이터를 애저 이벤트그리드로 보내는 액션을 만들어 보면 아래와 같습니다. 아래 그림에 보면 헤더 영역에 `ce-`로 시작하는 다양한 메타 데이터가 보이는데, 이는 CloudEvents 데이터 전송시 [교차 검증][ce binding http]과 관련된 규약 때문입니다.

![이벤트그리드로 데이터 보내기][image-07]

이렇게 이벤트그리드로 데이터를 보내고 나면 개별 소셜 미디어 처리기에서 이 이벤트 데이터를 처리할 준비가 된 셈인데요, 다음에서 개별 소셜 미디어 처리기가 어떻게 대응하는 지 볼 수 있습니다.

![이벤트그리드로 데이터 보내기 다이어그램][image-08]


### 2. 개별 소셜 미디어 처리기 ###

앞서 소셜 미디어에서 처리할 수 있을 정도로 데이터를 가공해서 이벤트그리드로 보냈다면, 개별 소셜 미디어 처리기는 각자 상황에 맞게 이 데이터를 받아 처리하면 됩니다. 이벤트그리드에서 받아온 데이터는 대략 아래와 같이 생겼습니다.

https://gist.github.com/justinyoo/cb04844306f9a44dcdcaaa70a6a55326?file=10-video-details-to-cloudevents.json


#### 트위터 ####

로직 앱은 [트위터 커넥터][az logapp connector twitter]를 자체 제공하고 있으므로 별도로 API 호출을 위한 코드를 만들 필요가 없습니다. 따라서, 아래와 같이 간단하게 호출하면 됩니다.

![트위터 포스팅하기][image-09]


#### 링크드인 ####

로직 앱은 [링크드인 커넥터][az logapp connector linkedin]도 자체 제공하고 있으므로 별도로 API 호출을 위한 코드를 만들 필요가 없습니다. 따라서, 아래와 같이 간단하게 호출하면 된다.

![링크드인 포스팅하기][image-10]


#### 페이스북 ####

반면에 로직 앱의 [페이스북 커넥터][az logapp connector facebook]는 더이상 사용할 수 없습니다. 따라서, [오픈소스로 풀린 커스텀 커넥터][az logapp connector facebook custom]를 이용하거나 다른 방법을 쓰는 수 밖에 없는데, 마침 [IFTTT][ifttt]에서 [페이스북 페이지로 포스팅][ifttt facebook page]하는 커넥터를 사용할 수 있으니, 이를 이용하기로 하겠습니다.

![IFTTT 페이스북 커넥터][image-11]

로직 앱 관점에서는 IFTTT 쪽으로 HTTP 요청만 보내면 되기 때문에 별다른 어려움은 없습니다. 다만, `value1`, `value2`, `value3` 어트리뷰트만 사용 가능하다는 점 주의하세요!

![페이스북 포스팅하기][image-12]

실제로 IFTTT 쪽에서 이 요청을 받아 처리한 결과는 아래와 같이 보입니다.

![IFTTT에서 페이스북 포스팅하기][image-14]

이렇게 해서 트위터, 링크드인, 페이스북 등 소셜 미디어로 새 유튜브 비디오가 올라왔을 경우 포스팅하는 방법을 구현해 보았습니다.

![E2E 이벤트 처리 절차][image-13]

---

지금까지 [애저 이벤트그리드][az evtgrd], [애저 펑션][az fncapp], [애저 로직 앱][az logapp] 등을 사용해서 특정 유튜브 채널에 새 비디오가 올라왔을 때, 이를 WebSub 이벤트로 받으면, 1) 이를 CloudEvents 형식으로 변환해서 애저 이벤트그리드로 보내고, 2) 필요한 형태로 변환한 후, 3) 원하는 소셜 미디어로 포스팅하는 전체적인 워크플로우를 구현해 보았습니다. 각각의 단계는 모두 디커플링을 시켜놓았기 때문에 유지 보수 차원에서 의존성을 고려할 필요가 없을 뿐더러, 향후 새로운 소셜 미디어 채널로 포스팅을 계획할 경우에도 손쉽게 추가할 수 있는 구조를 만들었습니다.

만약 이런 온라인 컨텐츠 마케팅을 기획하고 있다면 이와 비슷한 형태로 애플리케이션을 구현해 보는 것도 좋은 시도가 될 것입니다. 또한, 이를 통해 전체적인 클라우드 시스템 아키텍처를 구성하는 방법에 대해서도 고민해 볼 수 있을 것입니다.


## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][az account free]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt msdevkr]


[image-01]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-01.png
[image-02]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-02-ko.png
[image-03]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-03.png
[image-04]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-04.png
[image-05]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-05-ko.png
[image-06]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-06.png
[image-07]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-07.png
[image-08]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-08-ko.png
[image-09]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-09.png
[image-10]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-10.png
[image-11]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-11.png
[image-12]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-12.png
[image-13]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-13-ko.png
[image-14]: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/websub-to-eventgrid-via-cloudevents-and-beyond-14.png

[az account free]: https://azure.microsoft.com/ko-kr/free/?WT.mc_id=dotnet-12565-juyoo
[ms learn]: https://docs.microsoft.com/ko-kr/learn/?WT.mc_id=dotnet-12565-juyoo
[yt msdevkr]: https://www.youtube.com/channel/UCdgR-b2t7Byu_UGrHnu-T0g

[post prev 1]: /developerkorea/posts/2021/01/19/cloudevents-for-azure-eventgrid-via-azure-functions/

[gh sample]: https://github.com/devrel-kr/youtube-websub-subscription-handler

[pshb]: https://github.com/pubsubhubbub/PubSubHubbub
[websub]: https://www.w3.org/TR/websub/
[websub wd 1]: https://www.w3.org/TR/2016/WD-pubsub-20161020/
[websub verification]: https://indieweb.org/How_to_publish_and_consume_WebSub#The_hub_verifies_the_subscription_request

[eip pubsub]: https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html

[yt webhook]: https://developers.google.com/youtube/v3/guides/push_notifications
[yt websub hub]: https://pubsubhubbub.appspot.com/
[yt websub sub]: https://pubsubhubbub.appspot.com/subscribe

[ce]: https://cloudevents.io/
[ce binding http]: https://github.com/cloudevents/spec/blob/master/http-protocol-binding.md#3-http-message-mapping

[az evtgrd]: https://docs.microsoft.com/ko-kr/azure/event-grid/overview?WT.mc_id=devops-dotnet-12869-juyoo
[az evtgrd ce]: https://docs.microsoft.com/ko-kr/azure/event-grid/cloudevents-schema?WT.mc_id=devops-dotnet-12869-juyoo#use-with-azure-functions
[az evtgrd delivery auth]: https://docs.microsoft.com/ko-kr/azure/event-grid/security-authentication?WT.mc_id=devops-dotnet-12869-juyoo

[az logapp]: https://docs.microsoft.com/ko-kr/azure/logic-apps/logic-apps-overview?WT.mc_id=dotnet-12869-juyoo
[az logapp connector twitter]: https://docs.microsoft.com/ko-kr/connectors/twitter/?WT.mc_id=dotnet-12869-juyoo
[az logapp connector linkedin]: https://docs.microsoft.com/ko-kr/connectors/linkedinv2/?WT.mc_id=dotnet-12869-juyoo
[az logapp connector facebook]: https://docs.microsoft.com/ko-kr/connectors/facebook/?WT.mc_id=dotnet-12869-juyoo
[az logapp connector facebook custom]: https://github.com/microsoft/PowerPlatformConnectors/tree/master/custom-connectors/Facebook

[az fncapp]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview?WT.mc_id=dotnet-12869-juyoo
[az fncapp binding evtgrd]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-bindings-event-grid?WT.mc_id=dotnet-12869-juyoo

[ifttt]: https://ifttt.com/
[ifttt facebook page]: https://ifttt.com/facebook_pages
