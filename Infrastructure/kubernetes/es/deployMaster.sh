#helm repo add elastic https://helm.elastic.co
#helm repo update

kubectl create ns es8
helm install elasticsearch elastic/elasticsearch --namespace es8 --values nodes/master.yaml
