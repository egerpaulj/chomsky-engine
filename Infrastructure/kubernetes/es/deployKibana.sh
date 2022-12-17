#kubectl create ns kibana

helm install kibana elastic/kibana --namespace es8 --values kibanavals.yaml
