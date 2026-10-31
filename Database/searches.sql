CREATE TABLE IF NOT EXISTS searches
(
    id
    INT
    PRIMARY
    KEY
    AUTO_INCREMENT,
    query_text
    VARCHAR
(
    255
) NOT NULL DEFAULT '',
    animal_id INT NULL,
    searched_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_searches_searched_at
(
    searched_at
),
    INDEX idx_searches_animal
(
    animal_id
),
    CONSTRAINT fk_searches_animal FOREIGN KEY
(
    animal_id
) REFERENCES animals
(
    id
) ON DELETE SET NULL
    );