docker run -d --rm --network development_network --name config_server --link mongodb --link redis --link es crawler/config_server:$1
docker run -d --rm --network development_network --name webdriver_server --link nginx --link redis --link es crawler/webdriver_server:$1
docker run -d --rm --network development_network --name request_server --link webdriver_server --link redis --link es crawler/request_server:$1
docker run -d --rm --network development_network --name management_service --link request_server --link config_server --link redis --link es -v /home/user/docker/volumes/crawler:/App/RequestRepository crawler/management_service:$1
