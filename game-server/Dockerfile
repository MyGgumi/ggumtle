FROM openjdk:21-jdk-slim

COPY ./build/libs/*-latest.jar app.jar
COPY ./.env .env

ENTRYPOINT java -jar app.jar