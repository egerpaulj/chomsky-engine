kubectl create ns redis

helm repo add redis  https://charts.bitnami.com/bitnami
helm repo update


# kubectl apply -f pvc.yml

helm install redis redis/redis --namespace redis
