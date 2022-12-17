helm repo add jenkins https://charts.jenkins.io
helm repo update

kubectl create ns jenkins
kubectl apply -f pvc.yml
helm install jenkins jenkins/jenkins --namespace jenkins --values values.yaml
