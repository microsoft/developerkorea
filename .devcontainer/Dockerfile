FROM ubuntu:20.04

WORKDIR /home/

COPY . .

ENV TZ=Asia/Seoul
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

RUN bash ./setup.sh
