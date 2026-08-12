# Lectorio

Plataforma web académica para la gestión, lectura y compartición de libros en formato PDF. Permite a los usuarios registrarse, subir sus propios libros, leerlos mediante un lector integrado en línea y compartirlos con otros usuarios, organizando el catálogo mediante etiquetas de género y tipo de presentación.

## Funcionalidades principales

- Registro e inicio de sesión de usuarios.
- Subida de libros en formato PDF con datos asociados (título, autor, etiquetas).
- Lector de PDF en línea integrado, sin necesidad de descargar el archivo.
- Compartición de libros en PDF entre usuarios registrados.
- Catálogo visual de libros mediante tarjetas, con etiquetas de género y presentación.

## Stack tecnológico

- **ASP.NET** — framework de desarrollo backend bajo arquitectura MVC.
- **Bootstrap** — diseño e interfaz de usuario.
- **Supabase** — base de datos, autenticación (Supabase Auth) y almacenamiento de archivos (bucket `libros-pdf` para PDFs y bucket `portadas` para miniaturas de portada).

## Modelo de datos

El sistema se apoya en seis tablas principales:

| Tabla | Descripción |
|---|---|
| Usuarios | Perfil de cada usuario registrado, referenciando la identidad gestionada por Supabase Auth. |
| Libros | Información de cada libro: título, autor, archivo PDF, portada y estado de lectura. |
| Géneros | Catálogo de géneros literarios, mostrados como etiquetas asociadas a cada libro. |
| Presentación | Catálogo de formatos o tipos de presentación del libro (novela, cuento, cómic, etc.), mostrados como etiquetas. |
| Libros_Géneros | Tabla intermedia que relaciona un libro con uno o varios géneros. |
| Compartidos | Registro de qué libros han sido compartidos, por quién y con quién. |

## Creadores

- Gustavo Adolfo Retana Hernández
- Jimmy Ernesto Ramos Castañeda
