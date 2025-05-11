docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT=Production --name webdriver_server registry:5000/crawler/webdriver_server:main_$1
docker run -d --rm --network development_network -p 8882:443 -e ASPNETCORE_ENVIRONMENT=Production --name request_server registry:5000/crawler/request_server:main_$1
docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT=Production --name management_service -v /home/user/docker/volumes/crawler:/App/RequestRepository registry:5000/crawler/management_service:main_$1
#docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT=Production --name scheduler registry:5000/crawler/scheduler:main_$1

