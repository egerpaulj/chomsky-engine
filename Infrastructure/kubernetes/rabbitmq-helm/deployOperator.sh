kubectl create ns rabbit
kubectl apply -f pvc.yml

helm repo add rabbitmq https://charts.bitnami.com/bitnami
helm install rabbitmq-cluster rabbitmq/rabbitmq --namespace rabbit --values values.yaml



