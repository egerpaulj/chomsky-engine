helm repo add twuni https://helm.twun.io
helm repo update

kubectl create ns docker-reg
kubectl apply -f pvc.yml

helm install registry twuni/docker-registry --namespace docker-reg --values values.yaml
