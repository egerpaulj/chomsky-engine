helm repo add gitea-charts https://dl.gitea.io/charts/
helm repo update

kubectl create ns git
kubectl apply -f pvc.yml
helm install gitea gitea-charts/gitea --namespace git --values values.yaml
