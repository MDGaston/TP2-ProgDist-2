# TP1-ProgDist2

# Instrucciones Trabajo práctico inicial

Primero hay que abrir la consola y situarse en el directorio

```bash
cd .../Tp2TrackingApi
```

Una vez situados correr el docker compose

```bash
docker compose up
```

# Para probar el proyecto entrar importar la coleccion de Postman

Es importante primero ejecutar el endpoint de login, para realizar las pruebas iniciales loguear con el siguiente usuario y contraseña

```bash
{
  "username": "admin",
  "password": "admin"
}
```

# Una vez ejecutado esto se generara el token que debe de ser utilizado en las request a la app de usuarios.

Se puede guardar el Token obtenido en la variable en postman Token(principal sugerencia) o agregarlo directamente en cada pedido en la seccion de "Bearer Token" en la parte de Authorization

# Updates

En esta version del tp se agregaron los endpoints correspondientes para poder probar la app de track de usuarios y la app de la Blacklist, ademas se agrego una aplicacion de consola TrackingConsumer que sera el encargado de logear los mensajes de las colas y de indicar si es de estado critico en caso de que el usuario ingrese a una URL blacklisteada. Estos logs se pueden encontrar en:

\Tp2TrackingApi\Logs
