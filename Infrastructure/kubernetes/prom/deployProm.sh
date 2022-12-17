helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

kubectl create ns prom
kubectl apply -f pvc.yml
helm install prom prometheus-community/prometheus --namespace prom --values values.yaml
