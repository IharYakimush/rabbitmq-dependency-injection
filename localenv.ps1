docker kill $(docker ps -q)
docker-compose -p RabbitMq.Sample -f docker-compose.yaml up -d