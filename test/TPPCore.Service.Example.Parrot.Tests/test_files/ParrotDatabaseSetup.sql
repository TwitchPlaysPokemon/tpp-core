/* Instructions for setting up the database:
 * Install PostgreSQL v10
 * The program assumes that you have already set up the database
 * by ensuring that you have name the database what is in the config,
 * the host and port is correct, and the username and password is correct
 * the application name can be set to anything, but it helps identify the service
 */
 
--creation of the "parrot" table
CREATE TABLE IF NOT EXISTS parrot(
ID SERIAL PRIMARY KEY,
contents TEXT NOT NULL,
timestamp TIMESTAMP WITHOUT TIME ZONE DEFAULT (now() at time zone 'utc')
);

--the six following stored procedures must also be created

--parrot_insert
CREATE OR REPLACE FUNCTION parrot_insert(contents TEXT)
RETURNS VOID AS $$
BEGIN
INSERT INTO parrot(contents) VALUES(contents);
END; $$
LANGUAGE plpgsql;

--parrot_return_contents
CREATE OR REPLACE FUNCTION parrot_return_contents(_id INT)
RETURNS TEXT AS $$
BEGIN
RETURN (SELECT contents FROM parrot WHERE ID = _id);
END; $$
LANGUAGE plpgsql;

--parrot_return_timestamp
CREATE OR REPLACE FUNCTION parrot_return_timestamp(_id INT)
RETURNS TEXT AS $$
BEGIN
RETURN (SELECT cast(timestamp as TEXT) FROM parrot WHERE ID = _id);
END; $$
LANGUAGE plpgsql;

--parrot_delete
CREATE OR REPLACE FUNCTION parrot_delete()
RETURNS void AS $$
BEGIN
DELETE FROM parrot;
END; $$
LANGUAGE plpgsql;

--parrot_delete(_id int)
CREATE OR REPLACE FUNCTION parrot_delete(_id INT)
RETURNS VOID AS $$
BEGIN
DELETE FROM parrot WHERE ID = _id;
END; $$
LANGUAGE plpgsql;

--parrot_return_max_key
CREATE OR REPLACE FUNCTION parrot_return_max_key()
RETURNS INT AS $$
BEGIN
RETURN (SELECT CAST(ID as INT) FROM parrot ORDER BY ID DESC LIMIT 1);
END; $$
LANGUAGE plpgsql;
