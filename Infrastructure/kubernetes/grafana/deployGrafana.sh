helm repo add grafana https://grafana.github.io/helm-charts
kubectl create ns grafana
kubectl apply -f grafanaPvc.yml
helm install grafana grafana/grafana --namespace grafana --values grafana.yml
