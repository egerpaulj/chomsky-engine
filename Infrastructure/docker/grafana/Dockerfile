FROM grafana/grafana

COPY grafana/provisioning/dashboards.yml /etc/grafana/provisioning/dashboards/dashboards.yml
COPY grafana/provisioning/datasources.yml /etc/grafana/provisioning/datasources/datasources.yml
COPY ["Crawler Lite-1739030510366.json", "/var/lib/grafana/dashboards/crawler-lite.json"]
