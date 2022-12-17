helm repo add jupyterhub https://jupyterhub.github.io/helm-chart/
helm repo update

kubectl create ns jupyter
kubectl apply -f pvc.yaml

helm upgrade --cleanup-on-fail \
  --install jupyter jupyterhub/jupyterhub \
  --namespace jupyter \
  --version=2.0.0 \
  --values values.yaml
