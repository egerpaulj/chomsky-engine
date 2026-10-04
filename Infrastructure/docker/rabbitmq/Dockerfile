FROM rabbitmq:4-management

ENV RABBITMQ_DEFAULT_VHOST=crawler
ENV RABBITMQ_DEFAULT_USER=guest
ENV RABBITMQ_DEFAULT_PASS=guest

COPY rabbitmq/rabbitmq.conf /etc/rabbitmq/rabbitmq.conf
COPY rabbitmq/definitions.json /etc/rabbitmq/definitions.json
