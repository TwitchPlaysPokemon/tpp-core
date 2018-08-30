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
