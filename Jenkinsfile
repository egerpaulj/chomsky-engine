pipeline {
  agent any
  stages {
    stage('Conf Server') {
      steps {
        sh 'dotnet publish -c Release Crawler.Configuration/Crawler.Configuration.Server/Crawler.Configuration.Server.csproj'
      }
    }

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
        sh 'cp exportphrase Crawler.Configuration/Crawler.Configuration.Server/.'
        sh 'cp exportphrase Crawler.WebDriver/Crawler.WebDriver.Grpc.Server/.'
        sh 'cp exportphrase Crawler.RequestManager.Grpc.Server/.'
        sh 'openssl req -x509  -newkey rsa:4096 -keyout configserverkey.pem -out configservercert.pem -days 365 -passout file:passphrase -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=config_server"'
        sh 'openssl req -x509  -newkey rsa:4096 -keyout webdriverkey.pem -out webdrivercert.pem -days 365 -passout file:passphrase -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=webdriver_server"'
        sh 'openssl req -x509  -newkey rsa:4096 -keyout requestmanagerkey.pem -out requestmanagercert.pem -days 365 -passout file:passphrase -subj "/C=CH/ST=zurich/L=zurich/O=stgermain/OU=crawler/CN=request_server"'
        sh 'openssl pkcs12 -export -out Crawler.Configuration/Crawler.Configuration.Server/configserver.pfx -inkey configserverkey.pem -in configservercert.pem -passin file:passphrase -passout file:exportphrase'
        sh 'openssl pkcs12 -export -out Crawler.WebDriver/Crawler.WebDriver.Grpc.Server/webdriver.pfx -inkey webdriverkey.pem -in webdrivercert.pem -passin file:passphrase -passout file:exportphrase'
        sh 'openssl pkcs12 -export -out Crawler.RequestManager.Grpc.Server/requestmanager.pfx -inkey requestmanagerkey.pem -in requestmanagercert.pem -passin file:passphrase -passout file:exportphrase'
        sh 'cp configservercert.pem Crawler.Management.Service/.'
        sh 'cp requestmanagercert.pem Crawler.Management.Service/.'
        sh 'cp webdrivercert.pem Crawler.RequestManager.Grpc.Server/.'
        sh 'cp requestmanagercert.pem /usr/local/share/ca-certificates/requestmanager.crt'
        sh 'cp webdrivercert.pem /usr/local/share/ca-certificates/webdriver.crt'
        sh 'cp configservercert.pem /usr/local/share/ca-certificates/configserver.crt'
        sh 'update-ca-certificates'
      }
    }

    stage('Containerize') {
      steps {
        sh 'docker build --tag crawler/config_server:${BRANCH_NAME}_$BUILD_ID Crawler.Configuration/Crawler.Configuration.Server/.'
        sh 'docker build --tag registry:5000/crawler/webdriver_server:${BRANCH_NAME}_$BUILD_ID Crawler.WebDriver/Crawler.WebDriver.Grpc.Server/.'
        sh 'docker build --tag registry:5000/crawler/request_server:${BRANCH_NAME}_$BUILD_ID Crawler.RequestManager.Grpc.Server/.'
        sh 'docker build --tag registry:5000/crawler/management_service:${BRANCH_NAME}_$BUILD_ID Crawler.Management.Service/.'
        sh 'docker build --tag registry:5000/crawler/test_server:${BRANCH_NAME}_$BUILD_ID Crawler.IntegrationTest/Crawler.IntegrationTest.Server/.'
        sh 'docker build --tag registry:5000/crawler/scheduler:${BRANCH_NAME}_$BUILD_ID Crawler.Scheduler/Crawler.Scheduler.Service/.'
      }
    }



    stage('Shutdown Previous Crawler') {
      steps {
        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop test_server'
        }

        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop config_server'
        }

        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop webdriver_server'
        }

        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop management_service'
        }

        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop request_server'
        }

        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop scheduler'
        }

      }
    }

    

    stage('Bootstrap Test') {
      steps {
        sh 'docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT="Test" --name config_server crawler/config_server:${BRANCH_NAME}_$BUILD_ID'
        sh 'docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT="Test" --name webdriver_server registry:5000/crawler/webdriver_server:${BRANCH_NAME}_$BUILD_ID'
        sh 'docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT="Test" --name request_server registry:5000/crawler/request_server:${BRANCH_NAME}_$BUILD_ID'
        sh 'docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT="Test" --name management_service -v /home/user/docker/volumes/crawler:/App/RequestRepository registry:5000/crawler/management_service:${BRANCH_NAME}_$BUILD_ID'
        sh 'docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT="Test" --name test_server registry:5000/crawler/test_server:${BRANCH_NAME}_$BUILD_ID'
        sh 'docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT="Test" --name scheduler registry:5000/crawler/scheduler:${BRANCH_NAME}_$BUILD_ID'
      }
    }

    stage('Run Integration Tests') {
      steps {
        sh 'rm -rf TestResults'

        catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {sh '/usr/bin/dotnet test -o TestResults -r TestResults -l trx Crawler.IntegrationTest/Crawler.IntegrationTest/Crawler.IntegrationTest.csproj'}
        catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {sh '/usr/bin/dotnet test -o TestResults -r TestResults -l trx Crawler.Core/Crawler.Core.UnitTest/Crawler.Core.UnitTest.csproj'}
        catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {sh '/usr/bin/dotnet test -o TestResults -r TestResults -l trx Crawler.Core/Crawler.Management.Core.UnitTest/Crawler.Management.Core.UnitTest.csproj'}
        catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {sh '/usr/bin/dotnet test -o TestResults -r TestResults -l trx Crawler.Core/Crawler.Scheduler.Core.UnitTest/Crawler.Scheduler.Core.UnitTest.csproj'}
        catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {sh '/usr/bin/dotnet test -o TestResults -r TestResults -l trx Crawler.Core/Crawler.Stategies.Core.UnitTest/Crawler.Stategies.Core.UnitTest.csproj'}
        catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {sh '/usr/bin/dotnet test -o TestResults -r TestResults -l trx Crawler.Core/Crawler.Strategies.General.UnitTest/Crawler.Strategies.General.UnitTest.csproj'}
        catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {sh '/usr/bin/dotnet test -o TestResults -r TestResults -l trx Microservice.Amqp/Microservice.Amqp.Rabbitmq.Test/Microservice.Amqp.Rabbitmq.Test.csproj'}
        catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {sh '/usr/bin/dotnet test -o TestResults -r TestResults -l trx Microservice.Elasticsearch/Microservice.Elasticsearch.Test/Microservice.Elasticsearch.Test.csproj'}
        catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {sh '/usr/bin/dotnet test -o TestResults -r TestResults -l trx Microservice.Exchange/Microservice.Exchange.Test/Microservice.Exchange.Test.csproj'}
        catchError(buildResult: 'SUCCESS', stageResult: 'FAILURE') {sh '/usr/bin/dotnet test -o TestResults -r TestResults -l trx Microservice.Mongodb/Microservice.Mongodb.Test/Microservice.Mongodb.Test.csproj'}


        mstest(testResultsFile: 'Crawler.IntegrationTest/Crawler.IntegrationTest/TestResults/*.trx', failOnError: true)
        mstest(testResultsFile: 'Crawler.Core/Crawler.Core.UnitTest/TestResults/*.trx', failOnError: true)
        mstest(testResultsFile: 'Crawler.Core/Crawler.Management.Core.UnitTest/TestResults/*.trx', failOnError: true)
        mstest(testResultsFile: 'Crawler.Core/Crawler.Scheduler.Core.UnitTest/TestResults/*.trx', failOnError: true)
        mstest(testResultsFile: 'Crawler.Core/Crawler.Stategies.Core.UnitTest/TestResults/*.trx', failOnError: true)
        mstest(testResultsFile: 'Crawler.Core/Crawler.Strategies.General.UnitTest/TestResults/*.trx', failOnError: true)
        mstest(testResultsFile: 'Microservice.Amqp/Microservice.Amqp.Rabbitmq.Test/TestResults/*.trx', failOnError: true)
        mstest(testResultsFile: 'Microservice.Elasticsearch/Microservice.Elasticsearch.Test/TestResults/*.trx', failOnError: true)
        mstest(testResultsFile: 'Microservice.Exchange/Microservice.Exchange.Test/TestResults/*.trx', failOnError: true)
        mstest(testResultsFile: 'Microservice.Mongodb/Microservice.Mongodb.Test/TestResults/*.trx', failOnError: true)
      }
    }

    stage('Shutdown Test Crawler') {
      steps {
        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop test_server'
        }

        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop config_server'
        }

        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop webdriver_server'
        }

        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop management_service'
        }

        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop request_server'
        }

        catchError(buildResult: 'SUCCESS', stageResult: 'SUCCESS') {
          sh 'docker stop scheduler'
        }

      }
    }

    stage('Start Production Crawler') {
      steps {
        sh 'docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT="Production" --name webdriver_server registry:5000/crawler/webdriver_server:${BRANCH_NAME}_$BUILD_ID'
        sh 'docker run -d --rm --network development_network -p 8882:443 -e ASPNETCORE_ENVIRONMENT="Production" --name request_server registry:5000/crawler/request_server:${BRANCH_NAME}_$BUILD_ID'
        sh 'docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT="Production" --name management_service -v /home/user/docker/volumes/crawler:/App/RequestRepository registry:5000/crawler/management_service:${BRANCH_NAME}_$BUILD_ID'
        sh 'docker run -d --rm --network development_network -e ASPNETCORE_ENVIRONMENT="Production" --name scheduler registry:5000/crawler/scheduler:${BRANCH_NAME}_$BUILD_ID'
      }
    }
  }
  environment {
    TestBranchName = '${BRANCH_NAME}'
  }
}
