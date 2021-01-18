---
title: "애저 펑션과 애저 이벤트그리드간 CloudEvents 형식 이벤트 데이터 주고받기"
slug: cloudevents-for-azure-eventgrid-via-azure-functions
description: "이 포스트에서는 애저 펑션의 이벤트그리드 바인딩이 아직 지원하지 않는 CloudEvents 형식의 이벤트 데이터를 다루는 방법에 대해 알아봅니다."
date: 2021-01-19
image: https://sa0blogs.blob.core.windows.net/aliencube/2021/01/dealing-cloudevents-with-azure-functions-for-azure-eventgrid-00.png
image_caption: 애저 펑션에서 이벤트 그리드로, 다시 애저 펑션으로 클라우드 이벤트 형식 데이터 이동
author: justin-yoo
category: Azure
tags: azure-functions, azure-eventgrid, cloudevents, azure-sdk
canonical_url: https://blog.aliencube.org/ko/2021/01/13/dealing-cloudevents-with-azure-functions-for-azure-eventgrid/
featured: false
---

[애저 펑션][az fncapp]은 자체적으로 [애저 이벤트그리드][az evtgrd]에 대한 [바인딩 확장 기능][az fncapp binding evtgrd]을 제공하고 있어서 이벤트그리드로 이벤트 데이터를 아주 손쉽게 주고 받을 수 있습니다. 하지만, 현재 바인딩 확장 기능은 아직 정식으로 [CloudEvents][ce] 형식을 지원하지 않는데, 그 이유는 [현재 버전의 SDK][nuget evtgrd legacy]가 아직 CloudEvents 형식을 지원하지 않기 때문입니다. 아마도 [새 버전의 SDK][nuget evtgrd new]가 GA되는 시점이 되면 애저 펑션 확장 기능에서도 이를 지원하지 않을까 예상하는데요, 따라서, 그 때 까지는 CloudEvents 형식을 사용해서 이벤트그리드에 데이터를 주고 받기 위해서 별도의 작업을 해 줘야 합니다. 이 포스트에서는 그 방법에 대해 간단히 정리해 보도록 하겠습니다.

> 이 포스트에서는 [.NET SDK][az sdk evtgrd dotnet]를 대상으로 기술합니다. 다른 언어로도 SDK를 지원하고 있으니, 아래 링크를 클릭해서 새 SDK를 확인해 보세요!
>
> * [JavaScript][az sdk evtgrd js]
> * [Python][az sdk evtgrd python]
> * [Java][az sdk evtgrd java]


## 애저 이벤트그리드 SDK 프리뷰 버전 설치 ##

이 글을 쓰는 현재 새 버전의 애저 이벤트그리드 SDK 버전은 [`4.0.0-beta.4`][nuget evtgrd new]로, 아직 프리뷰 상태입니다. 이 프리뷰 버전의 SDK를 사용하면 CloudEvents 형식의 이벤트 데이터를 활용할 수 있죠. 먼저 아래 명령어를 통해 프리뷰 버전의 SDK를 설치합니다.

https://gist.github.com/justinyoo/8282de7244bccca562cd508e64d89470?file=01-dotnet-add-package.sh

이제 본격적으로 이벤트그리드 데이터를 작업해 보도록 하겠습니다!


## CloudEvents 형식으로 이벤트 데이터 보내기 ##

애저 CLI에서 이벤트그리드 명령어를 사용하기 위해서는 먼저 아래와 같이 [확장 기능][az cli extensions]을 설치해야 합니다.

https://gist.github.com/justinyoo/8282de7244bccca562cd508e64d89470?file=02-az-extension-add.sh

확장 기능을 설치하고 나면, 아래 [애저 CLI][az cli] 명령어를 통해 이벤트그리드 커스텀 토픽의 엔드포인트를 확인합니다.

https://gist.github.com/justinyoo/8282de7244bccca562cd508e64d89470?file=03-get-endpoint.sh

그리고, 접속 키는 아래 명령어를 이용해서 확인합니다.

https://gist.github.com/justinyoo/8282de7244bccca562cd508e64d89470?file=04-get-access-key.sh

위에서 확인한 엔드포인트와 접속 키를 이용해서 아래와 같이 애저 펑션 안에 이벤트그리드용 데이터 퍼블리셔 인스턴스를 만듭니다.

https://gist.github.com/justinyoo/8282de7244bccca562cd508e64d89470?file=05-create-publisher.cs

CloudEvents 형식의 데이터를 보내기 위해서는 이벤트 데이터 뿐만 아니라 몇 가지 다른 메타 데이터 정보가 필요합니다.

* `source`: 이벤트 발생자. 보통 URL 형식으로 작성합니다.
* `type`: 이벤트 타입. 이 값을 이용해서 이벤트를 구분합니다. 형식은 `com.example.someevent`와 비슷한 형태가 됩니다.
* `datacontenttype`: 항상 `application/cloudevents+json` 값으로 지정해 주면 됩니다.

이외에도 다른 메타 데이터 정보가 필요하지만, 나머지는 SDK에서 자동으로 처리해주니 여기서는 크게 상관하지 않습니다.

> CloudEvents 데이터 형식에 대한 내용을 좀 더 확인하고 싶다면, [이 링크][ce spec json]를 읽어보세요!

위 메타 데이터 정보를 이용해 아래와 같이 CloudEvents 데이터를 작성한 후 이벤트그리드로 보내면 됩니다.

https://gist.github.com/justinyoo/8282de7244bccca562cd508e64d89470?file=06-publish-event.cs

위의 방법을 통해 CloudEvents 형식의 이벤트 데이터를 이벤트그리드 커스텀 토픽으로 보낼 수 있게 됐습니다.


## CloudEvents 형식으로 이벤트 데이터 받기 ##

앞서 [애저 펑션][az fncapp]에서는 [이벤트그리드 바인딩 확장 기능의 제약][az fncapp binding evtgrd]이 있다고 했습니다. 때문에 CloudEvents 형식의 데이터를 받아 처리하기 위해서는 [HTTP 트리거][az fncapp trigger http]를 이용해야 하는데요, 이 트리거는 두 가지 요청을 동시에 처리해야 합니다.

* 이벤트 핸들러 엔드포인트 검증 요청
* 이벤트 데이터 처리


### 이벤트 핸들러 엔드포인트 검증 요청 ###

[CloudEvents의 웹훅 스펙][ce spec webhook]에 따르면 검증 요청은 `OPTIONS` 메소드를 이용하고 요청 헤더에 반드시 `WebHook-Request-Origin`를 포함합니다 (line #8). 따라서 이 검증 요청에 응답하기 위해서는 이 요청 헤더 값을 응답 헤더의 `WebHook-Allowed-Origin` 값에 실어 보내야 합니다 (line #9).

https://gist.github.com/justinyoo/8282de7244bccca562cd508e64d89470?file=07-validate-request.cs&highlights=8,9


### 이벤트 데이터 처리 ###

위와 같이 이벤트 핸들러로서 애저 펑션 엔드포인트 검증에 성공했다면, 애저 이벤트그리드는 앞으로 이벤트 데이터를 계속해서 동일한 엔드포인트로 `POST` 메소드를 이용해 보냅니다. 이 때, CloudEvents 이벤트 데이터 전체를 사용하고 싶다면 아래 코드의 `@event` 인스턴스를 이용하면 되고 (line #18), `data` 부분만 사용하려면 아래 코드와 같이 비직렬화해서 사용합니다 (line #19).

https://gist.github.com/justinyoo/8282de7244bccca562cd508e64d89470?file=08-handle-event.cs&highlights=18,19

위와 같이 CloudEvents 형식의 이벤트 데이터를 이벤트 토픽에서 애저 펑션으로 받아 처리할 수 있게 됐습니다.

---

지금까지 [애저 펑션][az fncapp]에서 [애저 이벤트그리드][az evtgrd]로 [CloudEvents][ce] 형식의 이벤트 데이터를 보내고 받는 방법에 대해 알아 보았습니다. 이 방법은 포스트 안에서 계속 언급했다시피, 현재 [바인딩 확장 기능][az fncapp binding evtgrd]은 아직 CloudEvents 형식을 지원하지 않기 때문에, 일종의 임시방편으로 사용하는 방법입니다. 조만간 새 버전의 확장 기능이 릴리즈 되기를 기대합니다.


## 더 궁금하다면... ##

* 애저 클라우드에 관심이 있으신가요? ➡️ [무료 애저 계정 생성하기][az account free]
* 애저 클라우드 무료 온라인 강의 코스를 들어 보세요! ➡️ [Microsoft Learn][ms learn]
* 마이크로소프트 개발자 유튜브 채널 ➡️ [Microsoft Developer Korea][yt msdevkr]


[az account free]: https://azure.microsoft.com/ko-kr/free/?WT.mc_id=dotnet-12565-juyoo
[az devops free]: https://azure.microsoft.com/ko-kr/services/devops/?WT.mc_id=dotnet-12565-juyoo
[ms learn]: https://docs.microsoft.com/ko-kr/learn/?WT.mc_id=dotnet-12565-juyoo
[yt msdevkr]: https://www.youtube.com/channel/UCdgR-b2t7Byu_UGrHnu-T0g

[az cli]: https://docs.microsoft.com/ko-kr/cli/azure/what-is-azure-cli?WT.mc_id=dotnet-12565-juyoo
[az cli extensions]: https://docs.microsoft.com/ko-kr/cli/azure/azure-cli-extensions-list?WT.mc_id=dotnet-12565-juyoo

[az fncapp]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-overview?WT.mc_id=dotnet-12565-juyoo
[az fncapp binding evtgrd]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-bindings-event-grid?WT.mc_id=dotnet-12565-juyoo
[az fncapp trigger http]: https://docs.microsoft.com/ko-kr/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=csharp&WT.mc_id=dotnet-12565-juyoo

[az evtgrd]: https://docs.microsoft.com/ko-kr/azure/event-grid/overview?WT.mc_id=dotnet-12565-juyoo
[az evtgrd topic custom]: https://docs.microsoft.com/ko-kr/azure/event-grid/custom-topics?WT.mc_id=dotnet-12565-juyoo

[nuget evtgrd legacy]: https://www.nuget.org/packages/Microsoft.Azure.EventGrid/
[nuget evtgrd new]: https://www.nuget.org/packages/Azure.Messaging.EventGrid/

[az sdk evtgrd dotnet]: https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/eventgrid/Azure.Messaging.EventGrid
[az sdk evtgrd js]: https://github.com/Azure/azure-sdk-for-js/tree/master/sdk/eventgrid/eventgrid
[az sdk evtgrd python]: https://github.com/Azure/azure-sdk-for-python/tree/master/sdk/eventgrid/azure-eventgrid
[az sdk evtgrd java]: https://github.com/Azure/azure-sdk-for-java/tree/master/sdk/eventgrid/azure-messaging-eventgrid

[ce]: https://cloudevents.io/
[ce spec json]: https://github.com/cloudevents/spec/blob/v1.0/json-format.md#23-examples
[ce spec webhook]: https://github.com/cloudevents/spec/blob/v1.0/http-webhook.md#4-abuse-protection
