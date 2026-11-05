-- Run on the live database before deploying: the API now reads and writes this column.
ALTER TABLE animals
    ADD COLUMN source_url VARCHAR(500) DEFAULT NULL;
