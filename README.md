The webserver part of the iot project

### Commands to build and push
dotnet publish -c Release

docker build -t iot_server_cs -f Dockerfile . 

docker save -o iot_server_cs.tar iot_server_cs

scp /path/to/directory/iot_server_cs.tar xxx@iot.philippevoet.dev:/iot_server/iot_server_cs.tar (replace xxx with username)

ssh xxx@iot.philippevoet.dev

cd ../iot_server

docker load -i iot_server_cs.tar

sudo docker run -d -p 8080:8080 iot_server_cs

