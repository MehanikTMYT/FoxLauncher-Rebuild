-- 1. СОЗДАНИЕ ПОЛЬЗОВАТЕЛЯ
DROP USER IF EXISTS 'MineDBUser'@'%';
CREATE USER 'MineDBUser'@'%' IDENTIFIED BY 'vdsTest!';

-- --------------------------------------------------------------------

-- 2. БАЗА ДАННЫХ ДЛЯ AUTHSERVER (аутентификация, пользователи, сессии, скины/плащи)
CREATE DATABASE IF NOT EXISTS launcher_auth_db;
USE launcher_auth_db;

-- Таблица пользователей (аутентификация, профиль игрока)
CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(255) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    email_confirmed BOOLEAN DEFAULT FALSE,
    email_confirmation_token VARCHAR(255) NULL,
    email_token_expiry DATETIME NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP NULL,
    current_skin_id INT NULL, -- Внешний ключ на auth_db.skins.id
    current_cape_id INT NULL, -- Внешний ключ на auth_db.capes.id
    is_user BOOLEAN DEFAULT TRUE,
    INDEX idx_username (username),
    INDEX idx_email (email),
    INDEX idx_current_skin (current_skin_id),
    INDEX idx_current_cape (current_cape_id),
    INDEX idx_is_user (is_user)
);

-- Таблица сессий (JWT, сессии для лаунчера)
CREATE TABLE sessions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL, -- Внешний ключ на auth_db.users.id
    access_token VARCHAR(512) NOT NULL UNIQUE,
    client_token VARCHAR(512) NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_used_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_user_id (user_id),
    INDEX idx_access_token (access_token(255)),
    INDEX idx_client_token (client_token(255)),
    INDEX idx_expires_at (expires_at)
);

-- Таблица скинов пользователей
CREATE TABLE skins (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL, -- Внешний ключ на auth_db.users.id
    file_name VARCHAR(500) NOT NULL,
    original_name VARCHAR(255) NOT NULL,
    upload_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_default BOOLEAN DEFAULT FALSE,
    hash VARCHAR(64) NULL,
    size BIGINT NULL,
    INDEX idx_user_id (user_id),
    INDEX idx_file_name_and_hash (file_name(191), hash)
);

-- Таблица плащей пользователей
CREATE TABLE capes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL, -- Внешний ключ на auth_db.users.id
    file_name VARCHAR(500) NOT NULL,
    original_name VARCHAR(255) NOT NULL,
    upload_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT FALSE,
    hash VARCHAR(64) NULL,
    size BIGINT NULL,
    INDEX idx_user_id (user_id),
    INDEX idx_file_name_and_hash (file_name(191), hash)
);

-- Внешние ключи для AuthServer
ALTER TABLE users ADD CONSTRAINT fk_users_current_skin_auth
    FOREIGN KEY (current_skin_id) REFERENCES skins(id)
    ON DELETE SET NULL;

ALTER TABLE users ADD CONSTRAINT fk_users_current_cape_auth
    FOREIGN KEY (current_cape_id) REFERENCES capes(id)
    ON DELETE SET NULL;

ALTER TABLE sessions ADD CONSTRAINT fk_sessions_user_auth
    FOREIGN KEY (user_id) REFERENCES users(id)
    ON DELETE CASCADE;

ALTER TABLE skins ADD CONSTRAINT fk_skins_user_auth
    FOREIGN KEY (user_id) REFERENCES users(id)
    ON DELETE CASCADE;

ALTER TABLE capes ADD CONSTRAINT fk_capes_user_auth
    FOREIGN KEY (user_id) REFERENCES users(id)
    ON DELETE CASCADE;

-- --------------------------------------------------------------------

-- 3. БАЗА ДАННЫХ ДЛЯ ADMINSERVER (профили, версии, файлы)
CREATE DATABASE IF NOT EXISTS launcher_admin_db;
USE launcher_admin_db;

-- Таблица профилей (клиенты лаунчера)
CREATE TABLE profiles (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE,
    description TEXT NULL,
    default_version_id INT NULL, -- Внешний ключ на admin_db.versions.id
    INDEX idx_name (name),
    INDEX idx_default_version (default_version_id)
);

-- Таблица версий (конкретные версии клиента)
CREATE TABLE versions (
    id INT AUTO_INCREMENT PRIMARY KEY,
    profile_id INT NOT NULL, -- Внешний ключ на admin_db.profiles.id
    name VARCHAR(255) NOT NULL,
    jar_path VARCHAR(500) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_profile_id (profile_id),
    INDEX idx_name (name),
    INDEX idx_profile_name (profile_id, name)
);

-- Таблица файлов (файлы, принадлежащие конкретной версии)
CREATE TABLE files (
    id INT AUTO_INCREMENT PRIMARY KEY,
    version_id INT NOT NULL, -- Внешний ключ на admin_db.versions.id
    file_path VARCHAR(1000) NOT NULL,
    hash VARCHAR(64) NOT NULL,
    size BIGINT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_version_id (version_id),
    INDEX idx_file_path (file_path(255)),
    INDEX idx_hash (hash)
);

-- Внешние ключи для AdminServer
ALTER TABLE profiles ADD CONSTRAINT fk_profiles_default_version_admin
    FOREIGN KEY (default_version_id) REFERENCES versions(id)
    ON DELETE SET NULL;

ALTER TABLE versions ADD CONSTRAINT fk_versions_profile_admin
    FOREIGN KEY (profile_id) REFERENCES profiles(id)
    ON DELETE CASCADE;

ALTER TABLE files ADD CONSTRAINT fk_files_version_admin
    FOREIGN KEY (version_id) REFERENCES versions(id)
    ON DELETE CASCADE;

-- --------------------------------------------------------------------

-- 4. БАЗА ДАННЫХ ДЛЯ FILESERVER (файлы и, возможно, метаданные для доступа)
CREATE DATABASE IF NOT EXISTS launcher_file_db;
USE launcher_file_db;

-- (Опционально) Таблица для метаданных файлов, если нужна отдельная логика
-- Пример: таблица для логирования загрузок
CREATE TABLE download_logs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    file_path VARCHAR(1000) NOT NULL,
    user_id INT NULL, -- Если нужно логировать, кто скачал (может быть из auth_db)
    version_id INT NULL, -- Если нужно логировать, к какой версии относится файл (из admin_db)
    downloaded_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_file_path (file_path(255)),
    INDEX idx_user_id (user_id),
    INDEX idx_version_id (version_id)
    -- Не ставим внешние ключи, так как user_id/version_id могут быть из других БД
);

-- --------------------------------------------------------------------

-- 5. ПРЕДОСТАВЛЕНИЕ ПРАВ ПОЛЬЗОВАТЕЛЮ
-- Предоставляем пользователю MineDBUser минимально необходимые права для работы приложения.
-- Эти права позволяют:
-- - Выполнять SELECT, INSERT, UPDATE, DELETE (DML) на все таблицы в указанных БД.
-- - Выполнять ALTER, CREATE, DROP (DDL) на таблицы (для миграций/обновлений схемы).
-- - EXECUTE для хранимых процедур/функций (если будут использоваться).
-- - CREATE TEMPORARY TABLES и LOCK TABLES (требуется EF Core и другими ORM).
-- - REFERENCES (для внешних ключей, хотя в MySQL это не строгое ограничение).
-- - INDEX (для создания индексов).

-- Права для launcher_auth_db
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, DROP, RELOAD, REFERENCES, INDEX, ALTER, CREATE TEMPORARY TABLES, LOCK TABLES, EXECUTE ON launcher_auth_db.* TO 'MineDBUser'@'%';

-- Права для launcher_admin_db
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, DROP, RELOAD, REFERENCES, INDEX, ALTER, CREATE TEMPORARY TABLES, LOCK TABLES, EXECUTE ON launcher_admin_db.* TO 'MineDBUser'@'%';

-- Права для launcher_file_db
GRANT SELECT, INSERT, UPDATE, DELETE, CREATE, DROP, RELOAD, REFERENCES, INDEX, ALTER, CREATE TEMPORARY TABLES, LOCK TABLES, EXECUTE ON launcher_file_db.* TO 'MineDBUser'@'%';

-- Обязательно применяем изменения
FLUSH PRIVILEGES;

-- --------------------------------------------------------------------

-- 6. СВЯЗИ МЕЖДУ БАЗАМИ (если они находятся на одном сервере)
-- Для проверки целостности или сложных запросов можно использовать представления или триггеры.
-- Пример: Представление в admin_db, показывающее имя пользователя, создавшего профиль (гипотетически)
-- USE launcher_admin_db;
-- CREATE VIEW profile_owners AS
-- SELECT p.id AS profile_id, p.name AS profile_name, u.username AS owner_username
-- FROM profiles p
-- JOIN launcher_auth_db.users u ON p.created_by_user_id = u.id; -- Поле created_by_user_id нужно добавить в profiles

-- Примечание: Реальные внешние ключи между БД не поддерживаются MySQL.
-- Для обеспечения целостности нужно полагаться на приложение или триггеры/процедуры.