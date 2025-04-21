import 'dart:io';
import 'dart:convert';

Map<String, dynamic> data = {
  "location":{
    'longitude': 4.42,
    'latitude': 51.184,
  },
  "sensors": [ // add time
    {'name': 'temperature', 'value': 65.0},
    {'name': 'humidity', 'value': 0.9},
    {'name': 'pressure', 'value':  6},
    {'name': 'light', 'value': 876},
  ],
};
List<WebSocket> clients = [];

void updateState() {
  for (var client in clients) {
    client.add(json.encode(data));
  }
}


dynamic getSensorData(String key) {
  data['sensors'].firstWhere((sensor) => sensor['name'] == key)['value'];
}

void setSensorData(String key, dynamic value) {
  try {
    data['sensors'].firstWhere((sensor) => sensor['name'] == key)['value'] = value;
  }
  catch (e) {
    List<Map<String, Object>> ddf = data['sensors'];
    ddf.add({'name': key, 'value': value});
    // data['sensors'] = ddf;
  }
  updateState();
}

void main() async {
  final port = 8080;

  final server = await HttpServer.bind(InternetAddress.anyIPv4, port);

  print('Server running on http://${server.address.host}:${server.port}');
  
  await for (HttpRequest request in server) {
    print(request.uri.path);
    if (request.uri.path == '/') {
      if (WebSocketTransformer.isUpgradeRequest(request)) {
        WebSocket socket = await WebSocketTransformer.upgrade(request);
        socket.listen(
              (message) {
            print("yay $message");
          },
          onDone: () => clients.removeWhere((client) => client == socket),
        );
        clients.add(socket);
        socket.add(json.encode(data));
        print(clients.length);
      }

      else {
        print("Request: ${request.method} ${request.uri}");
        final path = Uri.decodeComponent(request.uri.path == '/' ? 'index.html' : request.uri.path);
        final file = File('$path');

        if (await file.exists()) {
          final mimeType = _getMimeType(path);
          request.response.headers.contentType = ContentType.parse(mimeType);
          await request.response.addStream(file.openRead());
          request.response.close();
        } else {
          request.response
            ..statusCode = HttpStatus.notFound
            ..write('404 - Not Found');
        }

      }
    }
    else if (request.uri.path == '/location'){ linkLocation(request);}
    else {
      final sublinks = request.uri.path.split('/')..removeAt(0);
      print(sublinks);
      if (sublinks[0] == 'sensors') {
        linkSensors(request);
      }
      else {
        final mimetype = _getMimeType(sublinks[sublinks.length - 1]);
        print(mimetype);
        final path = Uri.decodeComponent(sublinks.join('/'));
        print(path);
        final file = File('$path');
        if (!await file.exists()) {
          request.response
            ..statusCode = HttpStatus.notFound
            ..write('404 - Not Found');
          request.response.close();

        }
        else {
          request.response.headers.contentType = ContentType.parse(mimetype);
          await request.response.addStream(file.openRead());
          request.response.close();
        }
      }
    }
  }
}


void linkSensors(HttpRequest request) async {
  final sublinks = request.uri.path.split('/')..removeAt(0); // 0 should be 'sensors'
  print('sensors');
  try {
    if (request.method == 'GET') {
      request.response.headers.contentType = ContentType.parse('text/plain; charset=utf-8');
      request.response.add(getSensorData(sublinks[1]).toString().codeUnits);
      request.response.close();
    }
    else if (request.method == 'POST') {
      setSensorData(sublinks[1], sublinks[2]);
      request.response.statusCode = HttpStatus.ok;
      request.response.close();
    }
  }
  catch (e){
    print('Error: $e');
    request.response
      ..statusCode = HttpStatus.badRequest
      ..write('Bad Request');
  }
}

void linkLocation(HttpRequest request) async {
  void setData(double latitude, double longitude) {
    data['location']['latitude'] = latitude;
    data['location']['longitude'] = longitude;
    updateState();
  }

  if (request.method == 'GET') {
    final queryParams = request.uri.queryParameters;
    final latitude = queryParams['latitude'];
    final longitude = queryParams['longitude'];
    if (latitude != null && longitude != null) {
      setData(latitude as double, longitude as double);
    }
    else {
      final file = File('location/index.html');
      request.response.headers.contentType = ContentType.parse('text/html; charset=utf-8');
      await request.response.addStream(file.openRead());
      request.response.close();
    }

  }
  else if(request.method == 'POST') {
    try {
      final content = await utf8.decoder.bind(request).join();
      final dat = json.decode(content);
      data['location']['latitude'] = double.parse(dat['latitude']);
      data['location']['longitude'] = double.parse(dat['longitude']);
      updateState();
      print(dat);
    }
    catch (e) {
      print('Error: $e');
    }
    finally {
      request.response
        ..statusCode = HttpStatus.ok
        ..write('Location updated');
      request.response.close();
    }
  }
}



String _getMimeType(String path) {
  if (path.endsWith('.html')) return 'text/html; charset=utf-8';
  if (path.endsWith('.css')) return 'text/css; charset=utf-8';
  if (path.endsWith('.js')) return 'application/javascript';
  if (path.endsWith('.json')) return 'application/json';
  if (path.endsWith('.png')) return 'image/png';
  if (path.endsWith('.jpg') || path.endsWith('.jpeg')) return 'image/jpeg';
  return 'text/plain; charset=utf-8';
}
