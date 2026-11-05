-- Run on the database before deploying: the API now reads and writes these columns.
ALTER TABLE animal_images
    ADD COLUMN attribution VARCHAR(500) DEFAULT NULL,
    ADD COLUMN source_page_url VARCHAR(500) DEFAULT NULL;