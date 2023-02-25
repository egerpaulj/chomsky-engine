pipeline {
  agent any
  stages {
    stage('Request Server') {
      steps {
        sh 'dotnet publish -c Release Crawler.RequestManager.Grpc.Server/Crawler.RequestManager.Grpc.Server.csproj'
      }
    }

    stage('WebDriver Server') {
      steps {
        sh 'dotnet publish -c Release Crawler.WebDriver/Crawler.WebDriver.Grpc.Server/Crawler.WebDriver.Grpc.Server.csproj'
      }
    }

    stage('Management Service') {
      steps {
        sh 'dotnet publish -c Release Crawler.Management.Service/Crawler.Management.Service.csproj'
      }
    }

    stage('Scheduler') {
      steps {
        sh 'dotnet publish -c Release Crawler.Scheduler/Crawler.Scheduler.Service/Crawler.Scheduler.Service.csproj'
      }
    }

    stage('Integration Test Server') {
      steps {
        sh 'dotnet publish -c Test Crawler.IntegrationTest/Crawler.IntegrationTest.Server/Crawler.IntegrationTest.Server.csproj'
      }
    }

    stage('Generate Certificates') {
      steps {
        sh 'openssl rand -base64 32 > passphrase'
        sh 'openssl rand -base64 32 > exportphrase'
        sh 'cp exportphrase Crawler.WebDriver/Crawler.WebDriver.Grpc.Server/.'
        sh 'cp exportphrase Crawler.RequestManager.Grpc.Server/.'
        sh 'openssl req -x509  -newkey rsa:4096 -keyout webdriverkey.pem -out webdrivercert.pem -days 365 -passout file:passphrase -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=webdriver_server"'
        sh 'openssl req -x509  -newkey rsa:4096 -keyout requestmanagerkey.pem -out requestmanagercert.pem -days 365 -passout file:passphrase -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=request_server"'
        sh 'openssl pkcs12 -export -out Crawler.WebDriver/Crawler.WebDriver.Grpc.Server/webdriver.pfx -inkey webdriverkey.pem -in webdrivercert.pem -passin file:passphrase -passout file:exportphrase'
        sh 'openssl pkcs12 -export -out Crawler.RequestManager.Grpc.Server/requestmanager.pfx -inkey requestmanagerkey.pem -in requestmanagercert.pem -passin file:passphrase -passout file:exportphrase'
        sh 'cp requestmanagercert.pem Crawler.Management.Service/.'
        sh 'cp webdrivercert.pem Crawler.RequestManager.Grpc.Server/.'
        sh 'cp requestmanagercert.pem /usr/local/share/ca-certificates/requestmanager.crt'
        sh 'cp webdrivercert.pem /usr/local/share/ca-certificates/webdriver.crt'
        sh 'update-ca-certificates'
      }
    }

    stage('Containerize') {
      steps {
        sh 'docker build --tag registry.localdomain:31309/crawler/webdriver_server:${BRANCH_NAME}_$BUILD_ID
        sh 'docker build --tag registry.localdomain:31309/crawler/request_server:${BRANCH_NAME}_$BUILD_ID
        sh 'docker build --tag registry.localdomain:31309/crawler/crawler_management:${BRANCH_NAME}_$BUILD_ID
        sh 'docker build --tag registry.localdomain:31309/crawler/test_server:${BRANCH_NAME}_$BUILD_ID
        sh 'docker build --tag registry.localdomain:31309/crawler/scheduler:${BRANCH_NAME}_$BUILD_ID
      }
    }

    stage('PushToRegistry') {
      steps {
        sh 'docker push registry.localdomain:31309/crawler/webdriver_server:${BRANCH_NAME}_$BUILD_ID
        sh 'docker push registry.localdomain:31309/crawler/request_server:${BRANCH_NAME}_$BUILD_ID
        sh 'docker push registry.localdomain:31309/crawler/crawler_management:${BRANCH_NAME}_$BUILD_ID
        sh 'docker push registry.localdomain:31309/crawler/test_server:${BRANCH_NAME}_$BUILD_ID
        sh 'docker push registry.localdomain:31309/crawler/scheduler:${BRANCH_NAME}_$BUILD_ID
      }
    }

    stage('Deploy to Kubernetes') {
      steps {
        sh 'kubectl apply -f crawler.yml'
      }
    }

  }
  environment {
    TestBranchName = '${BRANCH_NAME}'
  }
}
