FROM eclipse-temurin:21-jre-alpine

COPY ./build/libs/*-latest.jar app.jar
COPY ./.env .env

ENTRYPOINT java -jar app.jar