A very basic implementation of [RabbitMQs Direct Reply-to feature](https://www.rabbitmq.com/direct-reply-to.html) using RPC

This takes a given input message and reverses it in the response.

Before running, ensure RabbitMQ is running. This can be done with the Docker command:

`docker run -dp 5672:5672 rabbitmq`