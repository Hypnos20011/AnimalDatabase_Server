-- Run locally first; apply on the live database only after a backup and explicit approval.
ALTER TABLE animals
    ADD COLUMN IF NOT EXISTS length_mm_min DECIMAL(10,2) NULL,
    ADD COLUMN IF NOT EXISTS length_mm_max DECIMAL(10,2) NULL,
    ADD COLUMN IF NOT EXISTS height_mm_min DECIMAL(10,2) NULL,
    ADD COLUMN IF NOT EXISTS height_mm_max DECIMAL(10,2) NULL,
    ADD COLUMN IF NOT EXISTS weight_grams_min DECIMAL(14,3) NULL,
    ADD COLUMN IF NOT EXISTS weight_grams_max DECIMAL(14,3) NULL,
    ADD COLUMN IF NOT EXISTS lifespan_months_min DECIMAL(7,1) NULL,
    ADD COLUMN IF NOT EXISTS lifespan_months_max DECIMAL(7,1) NULL;

UPDATE animals
SET length_mm_min = ROUND(size_length_cm_min * 10, 2)
WHERE length_mm_min IS NULL AND size_length_cm_min IS NOT NULL;

UPDATE animals
SET length_mm_max = ROUND(size_length_cm_max * 10, 2)
WHERE length_mm_max IS NULL AND size_length_cm_max IS NOT NULL;

UPDATE animals
SET height_mm_min = ROUND(size_height_cm_min * 10, 2)
WHERE height_mm_min IS NULL AND size_height_cm_min IS NOT NULL;

UPDATE animals
SET height_mm_max = ROUND(size_height_cm_max * 10, 2)
WHERE height_mm_max IS NULL AND size_height_cm_max IS NOT NULL;

UPDATE animals
SET weight_grams_min = ROUND(weight_kg_min * 1000, 3)
WHERE weight_grams_min IS NULL AND weight_kg_min IS NOT NULL;

UPDATE animals
SET weight_grams_max = ROUND(weight_kg_max * 1000, 3)
WHERE weight_grams_max IS NULL AND weight_kg_max IS NOT NULL;

UPDATE animals
SET lifespan_months_min = ROUND(lifespan_years_min * 12, 1)
WHERE lifespan_months_min IS NULL AND lifespan_years_min IS NOT NULL;

UPDATE animals
SET lifespan_months_max = ROUND(lifespan_years_max * 12, 1)
WHERE lifespan_months_max IS NULL AND lifespan_years_max IS NOT NULL;

SELECT
    COUNT(CASE WHEN size_length_cm_min IS NOT NULL AND length_mm_min IS NULL THEN 1 END) AS length_mm_min_unmigrated,
    COUNT(CASE WHEN size_length_cm_max IS NOT NULL AND length_mm_max IS NULL THEN 1 END) AS length_mm_max_unmigrated,
    COUNT(CASE WHEN size_height_cm_min IS NOT NULL AND height_mm_min IS NULL THEN 1 END) AS height_mm_min_unmigrated,
    COUNT(CASE WHEN size_height_cm_max IS NOT NULL AND height_mm_max IS NULL THEN 1 END) AS height_mm_max_unmigrated,
    COUNT(CASE WHEN weight_kg_min IS NOT NULL AND weight_grams_min IS NULL THEN 1 END) AS weight_grams_min_unmigrated,
    COUNT(CASE WHEN weight_kg_max IS NOT NULL AND weight_grams_max IS NULL THEN 1 END) AS weight_grams_max_unmigrated,
    COUNT(CASE WHEN lifespan_years_min IS NOT NULL AND lifespan_months_min IS NULL THEN 1 END) AS lifespan_months_min_unmigrated,
    COUNT(CASE WHEN lifespan_years_max IS NOT NULL AND lifespan_months_max IS NULL THEN 1 END) AS lifespan_months_max_unmigrated
FROM animals;