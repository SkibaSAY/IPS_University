CREATE ROLE event_collector WITH
	LOGIN
	NOSUPERUSER
	NOCREATEDB
	NOCREATEROLE
	INHERIT
	NOREPLICATION
	NOBYPASSRLS
	CONNECTION LIMIT -1
	PASSWORD 'password';
COMMENT ON ROLE event_collector IS 'сборщик событий системы
может только писать в таблицы с собранными событиями

читать не может';

CREATE SCHEMA ids_events
    AUTHORIZATION postgres;

COMMENT ON SCHEMA ids_events
    IS 'собираемые события системы';

----
CREATE TABLE ids_events."Test"
(
    "@id" bigint NOT NULL,
    "#create_date" date NOT NULL DEFAULT NOW(),
    text text,
    PRIMARY KEY ("@id")
);

ALTER TABLE IF EXISTS ids_events."Test"
    OWNER to postgres;

GRANT INSERT ON ids_events."Test" TO event_collector;
