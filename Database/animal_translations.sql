CREATE TABLE animal_translations
(
    `animal_id`    int(11) NOT NULL,
    `language`     varchar(10) NOT NULL,
    `common_name`  varchar(120),
    `habitat`      varchar(255),
    `range_region` varchar(255),
    `profile`      text,
    `updated_at`   timestamp   NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),

    PRIMARY KEY (animal_id, language),
    CONSTRAINT FOREIGN KEY (`animal_id`) REFERENCES `animals` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;