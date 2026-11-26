ALTER TABLE animals
    ADD COLUMN status ENUM('pending','approved','rejected') NOT NULL DEFAULT 'approved',
    ADD COLUMN submitted_by ENUM('tester','admin') NOT NULL DEFAULT 'admin',
    ADD COLUMN reviewed_at TIMESTAMP NULL DEFAULT NULL,
    ADD COLUMN review_note VARCHAR(500) DEFAULT NULL,
    ADD INDEX idx_animals_status (status);