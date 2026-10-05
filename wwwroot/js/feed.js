// Mural: Me Gusta, Comentar y Ver más usando Fetch (sin recargar la página)

const feed = document.getElementById('feed');
const botonVerMas = document.getElementById('boton-ver-mas');
const finFeed = document.getElementById('fin-feed');
const errorFeed = document.getElementById('error-feed');
const moldePublicacion = document.getElementById('molde-publicacion');
const moldeComentario = document.getElementById('molde-comentario');
const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

const urls = {
    meGusta: feed.dataset.urlMegusta,
    comentar: feed.dataset.urlComentar,
    obtenerMas: feed.dataset.urlObtenerMas,
    login: feed.dataset.urlLogin,
    imagenDefault: feed.dataset.imagenDefault
};

// Cuántas publicaciones hay en pantalla = desde dónde pedir las siguientes
let desde = feed.querySelectorAll('.post').length;

// ============================================================ Ayudantes Fetch

// Lee el JSON de la respuesta. Si la sesión expiró manda al login;
// si el servidor devolvió un error, lanza una excepción con su mensaje.
async function leerRespuesta(respuesta) {
    const datos = await respuesta.json().catch(() => ({}));

    if (respuesta.status === 401) {
        window.location.href = urls.login;
        throw new Error(datos.error || 'Tu sesión expiró.');
    }
    if (!respuesta.ok) {
        throw new Error(datos.error || 'Algo salió mal. Probá de nuevo.');
    }
    return datos;
}

async function postear(url, datos) {
    const respuesta = await fetch(url, {
        method: 'POST',
        headers: { 'RequestVerificationToken': token },
        body: new URLSearchParams(datos) // application/x-www-form-urlencoded
    });
    return leerRespuesta(respuesta);
}

async function pedirJson(url) {
    const respuesta = await fetch(url, { headers: { 'Accept': 'application/json' } });
    return leerRespuesta(respuesta);
}

// ======================================================= Armado del DOM
// Siempre usamos textContent (nunca innerHTML) con datos de usuarios,
// así nadie puede inyectar HTML en un título o comentario.

function pintarMeGusta(post, leGusta, cantidad) {
    const boton = post.querySelector('.boton-like');
    boton.classList.toggle('activo', leGusta);
    boton.setAttribute('aria-pressed', leGusta);
    boton.querySelector('.like-etiqueta').textContent = leGusta ? 'Ya no me gusta' : 'Me gusta';
    post.querySelector('.like-cantidad').textContent = cantidad;
}

function pintarAvatar(elemento, inicial, tono) {
    elemento.textContent = inicial;
    elemento.style.setProperty('--tono', tono);
}

function crearComentario(c) {
    const nodo = moldeComentario.content.firstElementChild.cloneNode(true);
    pintarAvatar(nodo.querySelector('.comentario-avatar'), c.inicial, c.tono);
    nodo.querySelector('.comentario-usuario').textContent = c.nombreUsuario;
    nodo.querySelector('.comentario-texto').textContent = c.texto;
    nodo.querySelector('.comentario-fecha').textContent = c.fechaFormateada;
    return nodo;
}

function crearPublicacion(p) {
    const nodo = moldePublicacion.content.firstElementChild.cloneNode(true);
    nodo.dataset.id = p.id;

    pintarAvatar(nodo.querySelector('.post-avatar'), p.inicial, p.tono);
    nodo.querySelector('.post-usuario').textContent = p.nombreUsuario;
    nodo.querySelector('.post-fecha').textContent = p.fechaFormateada;

    const imagen = nodo.querySelector('.post-imagen');
    imagen.onerror = () => { imagen.onerror = null; imagen.src = urls.imagenDefault; };
    imagen.src = p.urlImagen;
    imagen.alt = p.titulo;

    nodo.querySelector('.post-titulo').textContent = p.titulo;
    nodo.querySelector('.post-descripcion').textContent = p.descripcion;
    pintarMeGusta(nodo, p.usuarioDioMeGusta, p.cantidadMeGusta);

    const lista = nodo.querySelector('.lista-comentarios');
    p.comentarios.forEach(c => lista.appendChild(crearComentario(c)));
    nodo.querySelector('.comentarios-cantidad').textContent = p.comentarios.length;

    return nodo;
}

// ================================================================ Me Gusta

async function alternarMeGusta(post) {
    const boton = post.querySelector('.boton-like');
    boton.disabled = true; // evita doble clic mientras esperamos al servidor

    try {
        const datos = await postear(urls.meGusta, { idPublicacion: post.dataset.id });
        pintarMeGusta(post, datos.leGusta, datos.cantidad);
        if (datos.leGusta) {
            boton.classList.remove('latido');
            void boton.offsetWidth; // reinicia la animación
            boton.classList.add('latido');
        }
    } catch (error) {
        mostrarErrorComentario(post, error.message);
    } finally {
        boton.disabled = false;
    }
}

// ================================================================ Comentar

function mostrarErrorComentario(post, mensaje) {
    const error = post.querySelector('.error-comentario');
    error.textContent = mensaje;
    error.hidden = !mensaje;
}

async function comentar(post, form) {
    const input = form.elements.texto;
    const boton = form.querySelector('button[type="submit"]');
    const texto = input.value.trim();

    mostrarErrorComentario(post, '');
    if (texto === '') {
        mostrarErrorComentario(post, 'El comentario no puede estar vacío.');
        return;
    }

    boton.disabled = true;
    try {
        const comentario = await postear(urls.comentar, {
            idPublicacion: post.dataset.id,
            texto: texto
        });

        const nuevo = crearComentario(comentario);
        nuevo.classList.add('recien');
        post.querySelector('.lista-comentarios').appendChild(nuevo);

        const contador = post.querySelector('.comentarios-cantidad');
        contador.textContent = Number(contador.textContent) + 1;
        input.value = '';
    } catch (error) {
        mostrarErrorComentario(post, error.message);
    } finally {
        boton.disabled = false;
        input.focus();
    }
}

// ================================================================= Ver más

async function verMas() {
    botonVerMas.disabled = true;
    botonVerMas.textContent = 'Cargando…';
    errorFeed.hidden = true;

    try {
        // GET /Publicacion/ObtenerMas?desde=10
        const datos = await pedirJson(`${urls.obtenerMas}?desde=${desde}`);

        datos.publicaciones.forEach(p => {
            // Si alguien publicó mientras tanto, el OFFSET se corre: evitamos duplicados
            if (!feed.querySelector(`.post[data-id="${p.id}"]`)) {
                feed.appendChild(crearPublicacion(p));
            }
        });
        desde += datos.publicaciones.length;

        if (!datos.hayMas) {
            botonVerMas.hidden = true;
            finFeed.hidden = false;
        }
    } catch (error) {
        errorFeed.textContent = error.message;
        errorFeed.hidden = false;
    } finally {
        botonVerMas.disabled = false;
        botonVerMas.textContent = 'Ver más';
    }
}

// ================================================================= Eventos
// Delegación: un solo listener en #feed funciona también para las
// publicaciones que se agregan después con "Ver más".

feed.addEventListener('click', (e) => {
    const boton = e.target.closest('.boton-like');
    if (boton) alternarMeGusta(boton.closest('.post'));
});

feed.addEventListener('submit', (e) => {
    const form = e.target.closest('.form-comentario');
    if (form) {
        e.preventDefault();
        comentar(form.closest('.post'), form);
    }
});

botonVerMas.addEventListener('click', verMas);
