---
title: "HTTP: сильный ETag и условное изменение If-Match"
description: "Технические рекомендации: HTTP: сильный ETag и условное изменение If-Match"
kind: standard
status: active
---

# HTTP: сильный ETag и условное изменение If-Match

**Опциональный профиль http-etag-concurrency.** Применяется только для ресурсов с явно принятым контрактом предотвращения потерянных обновлений на основе сильного ETag / If-Match. Базовый HTTP-стандарт не включает эту политику автоматически.

## Сетевой контракт

1. Сервер выдаёт ETag соответствующего представления. Клиент сохраняет непрозрачное значение целиком, вместе с кавычками. Слабый W/"..." не является сильным валидатором.
2. Клиент передаёт последний сильный валидатор в If-Match. Сервер выполняет **strong comparison** и атомарно связывает проверку с записью.
3. Устаревший либо слабый валидатор означает 412 Precondition Failed; отсутствующий при обязательном условии — 428 Precondition Required. Отдельный бизнес-конфликт может иметь 409 Conflict.
4. После успешной записи используйте новый ETag из ответа; не вычисляйте его на клиенте из полей модели.

## Примеры HTTP

Приведённые операции и значения — условные примеры, а не реальные адреса проекта.

## Реализация и проверка

Проверяйте успех, устаревшее/слабое условие, отсутствие If-Match, разные представления, конкурирующие записи и бизнес-конфликт. Две одновременные записи не должны успешно пройти одну и ту же проверку из-за гонки. Запишите реальные тесты и версию сервера.

## Сценарии на уровне протокола

```http scenario=read
GET /resources/42 HTTP/1.1
Host: api.example.test

HTTP/1.1 200 OK
Content-Type: application/json
ETag: "rev-7"

{"value":"before"}
```

```http scenario=success
PUT /resources/42 HTTP/1.1
Host: api.example.test
Content-Type: application/json
If-Match: "rev-7"

{"value":"after"}

HTTP/1.1 204 No Content
ETag: "rev-8"
```

```http scenario=stale
PUT /resources/42 HTTP/1.1
Host: api.example.test
If-Match: "rev-7"
Content-Type: application/json

{"value":"outdated"}

HTTP/1.1 412 Precondition Failed
```

```http scenario=weak
PUT /resources/42 HTTP/1.1
Host: api.example.test
If-Match: W/"rev-8"
Content-Type: application/json

{"value":"cannot-match-strongly"}

HTTP/1.1 412 Precondition Failed
```

```http scenario=missing
PUT /resources/42 HTTP/1.1
Host: api.example.test
Content-Type: application/json

{"value":"without-guard"}

HTTP/1.1 428 Precondition Required
```

```http scenario=conflict
PUT /resources/42 HTTP/1.1
Host: api.example.test
If-Match: "rev-8"
Content-Type: application/json

{"value":"violates-an-independent-constraint"}

HTTP/1.1 409 Conflict
```
