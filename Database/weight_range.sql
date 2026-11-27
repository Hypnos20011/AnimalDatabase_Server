-- Run on the live database before deploying: the API now accepts weights up to 9,999,999.99 kg.
ALTER TABLE animals
    MODIFY weight_kg_min DECIMAL(9, 2) DEFAULT NULL,
    MODIFY weight_kg_max DECIMAL(9, 2) DEFAULT NULL;
