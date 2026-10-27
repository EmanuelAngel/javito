-- Javito: estructura y datos de prueba.
--
-- Uso: mysql --table < database.sql
-- Recrea la base desde cero. Las fechas de los registros son relativas al día en que se ejecuta,
-- así que la semana en curso siempre tiene datos.
--
-- Usuarios de prueba (contraseña de todos: Javito2026)
--   admin@javito.local        Administrador
--   usuario@javito.local      Usuario, con los 14 hábitos reales
--   otra@javito.local         Usuario, para probar que nadie ve datos ajenos
--   desactivado@javito.local  Usuario desactivado: no puede iniciar sesión
--
-- Las reglas que caben en una fila son restricciones de la base (CHECK, UNIQUE, FOREIGN KEY): un
-- dato inválido hace fallar el script. Las que cruzan tablas las vigila db/validacion.sql.

-- La terminal de Windows habla cp850: sin esto, los acentos se guardan corruptos.
SET NAMES utf8mb4;

DROP DATABASE IF EXISTS javito;

CREATE DATABASE javito CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;

USE javito;


-- =============================================================================================
-- Estructura
-- =============================================================================================

CREATE TABLE usuario (
    id INT AUTO_INCREMENT PRIMARY KEY,
    rol ENUM('Administrador', 'Usuario') NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    -- La intercalación es insensible a mayúsculas: UNIQUE ya rechaza "Ana@x" si existe "ana@x".
    email VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    avatar_url VARCHAR(255) NULL,
    activo BOOLEAN NOT NULL
);


CREATE TABLE categoria (
    id INT AUTO_INCREMENT PRIMARY KEY,
    nombre VARCHAR(60) NOT NULL UNIQUE,
    descripcion VARCHAR(255) NULL,
    activo BOOLEAN NOT NULL
);


CREATE TABLE habito (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL,
    -- Un hábito puede quedar sin categoría (HU-HAB-05).
    categoria_id INT NULL,
    titulo VARCHAR(100) NOT NULL,
    descripcion VARCHAR(255) NULL,
    tipo_habito ENUM('si_no', 'contador', 'abstinencia') NOT NULL,
    meta_valor DECIMAL(8, 2) NULL,
    unidad_medida VARCHAR(30) NULL,
    frecuencia_tipo ENUM('diaria', 'semanal') NOT NULL,
    frecuencia_veces TINYINT NULL,
    activo BOOLEAN NOT NULL,
    CONSTRAINT fk_habito_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id),
    CONSTRAINT fk_habito_categoria FOREIGN KEY (categoria_id) REFERENCES categoria (id),
    INDEX ix_habito_usuario_activo (usuario_id, activo)
);


CREATE TABLE registro_habito (
    id INT AUTO_INCREMENT PRIMARY KEY,
    habito_id INT NOT NULL,
    fecha DATE NOT NULL,
    valor_alcanzado DECIMAL(8, 2) NOT NULL,
    -- La meta con la que se cerró el día: si el hábito cambia de meta, el historial no se reescribe.
    -- En sí/no y abstinencia vale 1.
    meta_objetivo DECIMAL(8, 2) NOT NULL,
    completado BOOLEAN NOT NULL,
    notas VARCHAR(500) NULL,
    creado_en DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_registro_habito FOREIGN KEY (habito_id) REFERENCES habito (id),
    -- Un registro por hábito por día: los toques del mismo día acumulan, no se duplican.
    CONSTRAINT uq_registro_habito_fecha UNIQUE (habito_id, fecha),
    -- Completado no se elige: se deriva del valor y la meta del día.
    CONSTRAINT ck_registro_completado CHECK (completado = (valor_alcanzado >= meta_objetivo)),
    INDEX ix_registro_fecha (fecha)
);


CREATE TABLE rutina (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL,
    nombre VARCHAR(60) NOT NULL,
    descripcion VARCHAR(255) NULL,
    activo BOOLEAN NOT NULL,
    CONSTRAINT fk_rutina_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id),
    INDEX ix_rutina_usuario_activo (usuario_id, activo)
);


CREATE TABLE rutina_habito (
    id INT AUTO_INCREMENT PRIMARY KEY,
    rutina_id INT NOT NULL,
    habito_id INT NOT NULL,
    orden INT NOT NULL,
    CONSTRAINT fk_rutina_habito_rutina FOREIGN KEY (rutina_id) REFERENCES rutina (id),
    CONSTRAINT fk_rutina_habito_habito FOREIGN KEY (habito_id) REFERENCES habito (id),
    -- El mismo hábito no entra dos veces en la misma rutina (HU-RUT-02).
    CONSTRAINT uq_rutina_habito UNIQUE (rutina_id, habito_id),
    CONSTRAINT ck_rutina_habito_orden CHECK (orden >= 1)
);


CREATE TABLE articulo (
    id INT AUTO_INCREMENT PRIMARY KEY,
    usuario_id INT NOT NULL,
    categoria_id INT NOT NULL,
    titulo VARCHAR(150) NOT NULL,
    resumen VARCHAR(300) NOT NULL,
    contenido_markdown MEDIUMTEXT NOT NULL,
    -- Un artículo sin portada se publica igual (HU-ART-01).
    imagen_portada_url VARCHAR(255) NULL,
    fecha_publicacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    activo BOOLEAN NOT NULL,
    CONSTRAINT fk_articulo_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id),
    CONSTRAINT fk_articulo_categoria FOREIGN KEY (categoria_id) REFERENCES categoria (id),
    INDEX ix_articulo_catalogo (activo, fecha_publicacion)
);


-- =============================================================================================
-- Datos de prueba
-- =============================================================================================

INSERT INTO usuario (id, rol, nombre, email, password_hash, activo) VALUES
    (1, 'Administrador', 'Administración Javito', 'admin@javito.local',
        '$2b$11$svgWvVzuj.RpO3KmgMZLPubDUdqfIAx4L/iSka0Cy/I6HjztIr0rm', TRUE),
    (2, 'Usuario', 'Usuario de prueba', 'usuario@javito.local',
        '$2b$11$dv2uGdOsumibppkxujCmhOmgx0MUP0w4wkCy1A8eqxjkeK5.3ui0C', TRUE),
    (3, 'Usuario', 'Otra cuenta', 'otra@javito.local',
        '$2b$11$cZrlihumCL4Bl0Euu92A.efUukH58DDkAz0qxR98W0luY1HpjKBna', TRUE),
    (4, 'Usuario', 'Cuenta desactivada', 'desactivado@javito.local',
        '$2b$11$SVXlz8Q5VD8H.TQ4PiKVgeaurH/evkhTdx9IoOWb26lnDgUVdHCee', FALSE);


INSERT INTO categoria (id, nombre, descripcion, activo) VALUES
    (1, 'Cuerpo', 'Movilidad, postura y actividad física', TRUE),
    (2, 'Mente', 'Atención, calma y reflexión', TRUE),
    (3, 'Alimentación', 'Lo que como y lo que tomo', TRUE),
    (4, 'Creatividad', 'Práctica de oficios y aficiones', TRUE),
    -- Desactivada: no aparece en el desplegable, pero quien ya la tenía la conserva (HU-HAB-05).
    (5, 'Espiritualidad', 'Prácticas contemplativas', FALSE);


-- Los 14 hábitos reales del usuario (PRODUCT.md), más tres ilustrativos para cubrir lo que la lista
-- real no tiene: un contador, una abstinencia y un hábito archivado.
INSERT INTO habito
    (id, usuario_id, categoria_id, titulo, descripcion, tipo_habito, meta_valor, unidad_medida,
        frecuencia_tipo, frecuencia_veces, activo)
VALUES
    (1, 2, 1, 'Liberación de cuello y hombros', 'Círculos lentos de hombros y cuello, 1 minuto',
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (2, 2, 1, 'Estiramiento de muñeca y antebrazo', 'Flexión y extensión de muñeca, 30 segundos por lado',
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (3, 2, 1, 'Extensión de espalda de pie', 'Manos en la cintura, arquear la espalda 5 veces',
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (4, 2, 1, 'Caminata corta por la habitación', NULL,
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (5, 2, 2, 'Respiración cuadrada', 'Inhalar 4, sostener 4, exhalar 4, sostener 4',
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (6, 2, 1, 'Descanso visual 20-20-20', 'Cada 20 minutos, mirar a 20 pies durante 20 segundos',
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (7, 2, 2, 'Anclaje sensorial 5-4-3-2-1', 'Nombrar 5 cosas que veo, 4 que toco, 3 que oigo, 2 que huelo, 1 que saboreo',
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (8, 2, 2, 'Reset mental y reflexión', NULL,
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (9, 2, 3, 'Hidratación con agua', NULL,
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    -- Sin categoría (HU-HAB-05).
    (10, 2, NULL, 'Té de hierbas consciente', NULL,
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    -- En una categoría que después se desactivó: la conserva.
    (11, 2, 5, 'Meditar', '10 minutos sentado, atención en la respiración',
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (12, 2, 1, 'Ejercicio pesado', 'Rutina de fuerza completa',
        'si_no', NULL, NULL, 'semanal', 3, TRUE),
    -- Hizo una semana, pausó dos y retomó esta.
    (13, 2, 4, 'Practicar dibujo', NULL,
        'si_no', NULL, NULL, 'semanal', 2, TRUE),
    (14, 2, 4, 'Practicar cocina', 'Una receta nueva',
        'si_no', NULL, NULL, 'semanal', 1, TRUE),
    -- Ilustrativos.
    (15, 2, 2, 'Leer', NULL,
        'contador', 10, 'páginas', 'diaria', NULL, TRUE),
    (16, 2, 1, 'No fumar', 'Tildar significa: hoy me mantuve',
        'abstinencia', NULL, NULL, 'diaria', NULL, TRUE),
    (17, 2, 1, 'Correr', NULL,
        'si_no', NULL, NULL, 'semanal', 2, FALSE),
    -- Hábitos de otras cuentas.
    (18, 3, 1, 'Tender la cama', NULL,
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (19, 3, 2, 'Respirar antes de responder', NULL,
        'si_no', NULL, NULL, 'diaria', NULL, TRUE),
    (20, 4, 1, 'Respiración nocturna', NULL,
        'si_no', NULL, NULL, 'diaria', NULL, TRUE);


INSERT INTO rutina (id, usuario_id, nombre, descripcion, activo) VALUES
    (1, 2, 'Mañana', 'Antes de arrancar el día', TRUE),
    (2, 2, 'Corte de escritorio', 'Pausas entre bloques de trabajo', TRUE),
    (3, 2, 'Noche', 'Para cerrar el día', TRUE),
    -- Desactivada: no aparece en la tira, pero conserva sus hábitos y su orden (HU-RUT-05).
    (4, 2, 'Vacaciones', NULL, FALSE),
    (5, 3, 'Arranque', NULL, TRUE);


-- Hidratación (9) está en dos rutinas a la vez. Ejercicio, dibujo, cocina, té y no fumar no están en
-- ninguna: son los "Sin rutina". Correr (17) está archivado dentro de Noche.
INSERT INTO rutina_habito (rutina_id, habito_id, orden) VALUES
    (1, 9, 1), (1, 1, 2), (1, 5, 3), (1, 11, 4),
    (2, 2, 1), (2, 3, 2), (2, 4, 3), (2, 6, 4), (2, 9, 5),
    (3, 7, 1), (3, 8, 2), (3, 15, 3), (3, 17, 4),
    (4, 4, 1), (4, 9, 2),
    (5, 18, 1), (5, 19, 2);


-- Registros de las últimas cuatro semanas: tres completas más la semana en curso hasta hoy.
-- dias tiene una fila por día, con la semana relativa (-3 a 0) y el día de la semana (0 = lunes).
SET @lunes = CURDATE() - INTERVAL WEEKDAY(CURDATE()) DAY;

CREATE TEMPORARY TABLE dias (
    idx INT PRIMARY KEY,
    fecha DATE NOT NULL,
    semana INT NOT NULL,
    dia_semana INT NOT NULL
);

INSERT INTO dias (idx, fecha, semana, dia_semana)
WITH RECURSIVE n (i) AS (
    SELECT 0
    UNION ALL
    SELECT i + 1 FROM n WHERE i < 27
)
SELECT i, @lunes - INTERVAL 21 DAY + INTERVAL i DAY, FLOOR(i / 7) - 3, MOD(i, 7)
FROM n
WHERE @lunes - INTERVAL 21 DAY + INTERVAL i DAY <= CURDATE();


-- Diarios sí/no: un patrón fijo que cumple unos 7 de cada 10 días, distinto por hábito.
INSERT INTO registro_habito (habito_id, fecha, valor_alcanzado, meta_objetivo, completado)
SELECT h.id, d.fecha, 1, 1, TRUE
FROM habito h
CROSS JOIN dias d
WHERE h.usuario_id = 2
    AND h.tipo_habito = 'si_no'
    AND h.frecuencia_tipo = 'diaria'
    AND MOD(h.id * 7 + d.idx * 3, 10) < 7;


-- Semanales: días elegidos a mano para que cada semana cuente una historia.
--   Ejercicio pesado (3/semana): cumple, se pasa de la cuota, cumple y esta semana va 2 de 3.
--   Practicar dibujo (2/semana): cumple, pausa dos semanas y retoma con 1 de 2.
--   Practicar cocina (1/semana): una vez cada semana, en días distintos.
--   Correr (2/semana, hoy archivado): dos semanas de historial antes de archivarlo.
INSERT INTO registro_habito (habito_id, fecha, valor_alcanzado, meta_objetivo, completado)
SELECT s.habito_id, @lunes + INTERVAL (s.semana * 7 + s.dia_semana) DAY, 1, 1, TRUE
FROM (
    VALUES
        ROW(12, -3, 0), ROW(12, -3, 2), ROW(12, -3, 4),
        ROW(12, -2, 0), ROW(12, -2, 1), ROW(12, -2, 3), ROW(12, -2, 5),
        ROW(12, -1, 0), ROW(12, -1, 2), ROW(12, -1, 4),
        ROW(12, 0, 0), ROW(12, 0, 2),
        ROW(13, -3, 1), ROW(13, -3, 5),
        ROW(13, 0, 1),
        ROW(14, -3, 6), ROW(14, -2, 5), ROW(14, -1, 6), ROW(14, 0, 2),
        ROW(17, -3, 1), ROW(17, -3, 3), ROW(17, -2, 2)
) AS s (habito_id, semana, dia_semana)
WHERE @lunes + INTERVAL (s.semana * 7 + s.dia_semana) DAY <= CURDATE();


-- Leer (contador, 10 páginas): la meta era 8 hasta hace dos semanas. Los días viejos conservan la meta
-- con la que se cerraron. Hay días sin llegar, días justos y días pasados de la meta.
INSERT INTO registro_habito (habito_id, fecha, valor_alcanzado, meta_objetivo, completado)
SELECT 15, v.fecha, v.valor, v.meta, v.valor >= v.meta
FROM (
    SELECT d.fecha, MOD(d.idx * 7, 16) AS valor, IF(d.semana < -1, 8, 10) AS meta
    FROM dias d
) AS v
WHERE v.valor > 0;


-- No fumar (abstinencia): días limpios, salvo dos recaídas.
INSERT INTO registro_habito (habito_id, fecha, valor_alcanzado, meta_objetivo, completado)
SELECT 16, d.fecha, 1, 1, TRUE
FROM dias d
WHERE d.idx NOT IN (4, 11);


-- Un día que se marcó y se desmarcó, pero quedó con nota: el registro existe sin estar completado.
INSERT INTO registro_habito (habito_id, fecha, valor_alcanzado, meta_objetivo, completado, notas)
SELECT 16, d.fecha, 0, 1, FALSE, 'Fumé uno después de la cena'
FROM dias d
WHERE d.idx = 11;


UPDATE registro_habito
SET notas = 'Lo hice antes de la reunión y ayudó'
WHERE habito_id = 5
ORDER BY fecha DESC
LIMIT 1;

UPDATE registro_habito
SET notas = 'Sentadilla con más peso'
WHERE habito_id = 12 AND fecha = @lunes;


-- Otras cuentas: algunos días, para comprobar que no se mezclan con los del usuario de prueba.
INSERT INTO registro_habito (habito_id, fecha, valor_alcanzado, meta_objetivo, completado)
SELECT h.id, d.fecha, 1, 1, TRUE
FROM habito h
CROSS JOIN dias d
WHERE h.usuario_id IN (3, 4)
    AND MOD(d.idx, 2) = 0;

DROP TEMPORARY TABLE dias;


INSERT INTO articulo
    (usuario_id, categoria_id, titulo, resumen, contenido_markdown, imagen_portada_url, fecha_publicacion, activo)
VALUES
    (1, 2, 'Por qué no hay racha en Javito',
        'La racha castiga el día que fallaste en lugar de premiar las semanas que sostuviste.',
        '# Por qué no hay racha\n\nUna racha **se corta**. Y cuando se corta, la motivación cae con ella.\n\n- Mirá la semana, no el día.\n- Un día vacío no borra los anteriores.\n',
        NULL, NOW() - INTERVAL 20 DAY, TRUE),
    (1, 1, 'Pausas activas en el escritorio',
        'Movimientos de un minuto para cortar las horas sentado.',
        '# Pausas activas\n\nCada **50 minutos**, levantate:\n\n1. Cuello y hombros\n2. Muñecas\n3. Espalda\n',
        NULL, NOW() - INTERVAL 12 DAY, TRUE),
    (1, 2, 'Hábitos semanales sin culpa',
        'Tres veces por semana no son cuatro días de fracaso.',
        '# Cupo semanal\n\nLa semana se cumple al llegar a la cuota, **sin importar qué días**.\n',
        NULL, NOW() - INTERVAL 5 DAY, TRUE),
    -- Desactivado: no aparece en el catálogo (HU-ART-03).
    (1, 3, 'Borrador sobre hidratación',
        'Un artículo retirado.',
        '# Hidratación\n\nPendiente de revisión.\n',
        NULL, NOW() - INTERVAL 2 DAY, FALSE);
