/*M!999999\- enable the sandbox mode */ 
-- MariaDB dump 10.19-11.8.3-MariaDB, for Win64 (AMD64)
--
-- Host: 127.0.0.1    Database: animaldatabase
-- ------------------------------------------------------
-- Server version	11.8.3-MariaDB

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*M!100616 SET @OLD_NOTE_VERBOSITY=@@NOTE_VERBOSITY, NOTE_VERBOSITY=0 */;

--
-- Table structure for table `animal_images`
--

DROP TABLE IF EXISTS `animal_images`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `animal_images` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `animal_id` int(11) NOT NULL,
  `kind` varchar(20) NOT NULL,
  `url` varchar(500) NOT NULL,
  `caption` varchar(200) DEFAULT NULL,
  `sort_order` int(11) NOT NULL DEFAULT 0,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `idx_animal_images_animal` (`animal_id`),
  CONSTRAINT `animal_images_ibfk_1` FOREIGN KEY (`animal_id`) REFERENCES `animals` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=20 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `animal_images`
--

LOCK TABLES `animal_images` WRITE;
/*!40000 ALTER TABLE `animal_images` DISABLE KEYS */;
set autocommit=0;
INSERT INTO `animal_images` VALUES
(1,1,'female','https://thumb.wikimedia.org/wikipedia/commons/thumb/b/b0/Bengal_tiger_%28Panthera_tigris_tigris%29_female_3_crop.jpg/1280px-Bengal_tiger_%28Panthera_tigris_tigris%29_female_3_crop.jpg','Bengal tiger female',1,'2026-10-08 12:05:39'),
(2,2,'unspecified','https://upload.wikimedia.org/wikipedia/commons/9/9f/Okapia_johnstoni_17zz.jpg','Okapi at a zoo',1,'2026-10-08 12:05:39'),
(3,3,'unspecified','https://thumb.wikimedia.org/wikipedia/commons/thumb/c/ca/Pardofelis_marmorata_-_James_Eaton_-_339015539_%28cropped%29.jpeg/1280px-Pardofelis_marmorata_-_James_Eaton_-_339015539_%28cropped%29.jpeg','Marbled cat',1,'2026-10-08 12:05:39'),
(4,11,'unspecified','https://thumb.wikimedia.org/wikipedia/commons/thumb/0/00/Axolotl_ganz.jpg/1280px-Axolotl_ganz.jpg','Axolotl',1,'2026-10-08 13:42:48'),
(5,12,'unspecified','https://upload.wikimedia.org/wikipedia/commons/8/89/Manis_crassicaudata_%2829600051791%29.jpg','Indian pangolin',1,'2026-10-08 13:58:49'),
(6,14,'female','https://upload.wikimedia.org/wikipedia/commons/6/61/Heteropoda_maxima_1.jpg','Giant Huntsman spider',1,'2026-10-09 06:07:23'),
(7,17,'unspecified','https://thumb.wikimedia.org/wikipedia/commons/thumb/2/24/Balaeniceps_rex_-Ueno_Zoo%2C_Tokyo%2C_Japan-8a.jpg/1280px-Balaeniceps_rex_-Ueno_Zoo%2C_Tokyo%2C_Japan-8a.jpg','Shoebill at Ueno Zoo',0,'2026-10-23 10:20:14'),
(8,18,'unspecified','https://thumb.wikimedia.org/wikipedia/commons/thumb/2/21/Pink_Fairy_Armadillo_%28Chlamyphorus_truncatus%29.jpg/1280px-Pink_Fairy_Armadillo_%28Chlamyphorus_truncatus%29.jpg','Pink fairy armadillo',0,'2026-10-23 10:20:14'),
(9,19,'unspecified','https://upload.wikimedia.org/wikipedia/commons/e/ef/Condylura.jpg','Star-nosed mole showing its tentacled nose',0,'2026-10-23 10:20:15'),
(10,20,'unspecified','https://upload.wikimedia.org/wikipedia/commons/f/f6/Webs180130-rat.jpg','Naked mole-rat',0,'2026-10-23 10:20:15'),
(11,21,'unspecified','https://thumb.wikimedia.org/wikipedia/commons/thumb/a/a2/Wild_aye_aye.jpg/1280px-Wild_aye_aye.jpg','Wild aye-aye on a branch',0,'2026-10-23 10:20:15'),
(12,22,'unspecified','https://upload.wikimedia.org/wikipedia/commons/9/97/Psychrolutes_phrictus_1.jpg','Blob sculpin (Psychrolutes phrictus), a close relative. Photo: NOAA/MBARI, public domain',0,'2026-10-23 10:20:15'),
(13,23,'unspecified','https://thumb.wikimedia.org/wikipedia/commons/thumb/e/e4/Leafy_Seadragon_on_Kangaroo_Island.jpg/1280px-Leafy_Seadragon_on_Kangaroo_Island.jpg','Leafy seadragon off Kangaroo Island',0,'2026-10-23 10:20:15'),
(14,24,'unspecified','https://upload.wikimedia.org/wikipedia/commons/6/62/Hyalinobatrachium_fleischmanni01a.jpg','Fleischmann\'s glass frog on a leaf',0,'2026-10-23 10:20:15'),
(15,25,'unspecified','https://thumb.wikimedia.org/wikipedia/commons/thumb/7/7f/2009_Chelus_fimbriatus.JPG/1280px-2009_Chelus_fimbriatus.JPG','Mata mata turtle in an aquarium',0,'2026-10-23 10:20:15'),
(16,26,'unspecified','https://upload.wikimedia.org/wikipedia/commons/b/b0/Dumbo-hires_%28cropped%29.jpg','Dumbo octopus in the deep sea',0,'2026-10-23 10:20:15'),
(18,27,'unspecified','https://upload.wikimedia.org/wikipedia/commons/f/f2/Hapalochlaena_lunulata2.JPG','Greater blue-ringed octopus (Hapalochlaena lunulata) in North Sulawesi',0,'2026-10-23 12:11:51'),
(19,28,'unspecified','https://thumb.wikimedia.org/wikipedia/commons/thumb/6/67/Madoqua_kirkii_-_male_%28Namutoni%29.jpg/1280px-Madoqua_kirkii_-_male_%28Namutoni%29.jpg','Other',0,'2026-10-23 13:10:09');
/*!40000 ALTER TABLE `animal_images` ENABLE KEYS */;
UNLOCK TABLES;
commit;

--
-- Table structure for table `animals`
--

DROP TABLE IF EXISTS `animals`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `animals` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `common_name` varchar(120) NOT NULL,
  `scientific_name` varchar(150) DEFAULT NULL,
  `family` varchar(120) DEFAULT NULL,
  `taxonomic_class` varchar(40) DEFAULT NULL,
  `is_domesticated` tinyint(1) NOT NULL DEFAULT 0,
  `size_length_cm_min` decimal(7,1) DEFAULT NULL,
  `size_length_cm_max` decimal(7,1) DEFAULT NULL,
  `size_height_cm_min` decimal(7,1) DEFAULT NULL,
  `size_height_cm_max` decimal(7,1) DEFAULT NULL,
  `weight_kg_min` decimal(7,2) DEFAULT NULL,
  `weight_kg_max` decimal(7,2) DEFAULT NULL,
  `lifespan_years_min` int(11) DEFAULT NULL,
  `lifespan_years_max` int(11) DEFAULT NULL,
  `iucn_status` varchar(40) DEFAULT NULL,
  `habitat` varchar(255) DEFAULT NULL,
  `range_region` varchar(255) DEFAULT NULL,
  `profile` text DEFAULT NULL,
  `created_at` timestamp NOT NULL DEFAULT current_timestamp(),
  `updated_at` timestamp NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=29 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `animals`
--

LOCK TABLES `animals` WRITE;
/*!40000 ALTER TABLE `animals` DISABLE KEYS */;
set autocommit=0;
INSERT INTO `animals` VALUES
(1,'Tiger','Panthera tigris','Felidae','mammal',0,140.0,280.0,80.0,110.0,100.00,260.00,10,15,'endangered','rainforests, grasslands, savannas, mangrove swamps','Asia (India, Nepal, Russia / Siberia)','Nocturnal and solitary. Seasonal and dispersal migrations between dry and wet season habitats.\n\nFun fact: No two tigers share the same stripe pattern, and the mostly vertical stripes help it vanish among tall grass, reeds and trees.','2026-10-08 08:04:49','2026-10-23 11:42:53'),
(2,'Okapi','Okapia johnstoni','Giraffidae','mammal',0,180.0,250.0,140.0,170.0,200.00,350.00,20,30,'endangered','canopy forest','Congo basin, Africa','Diurnal and solitary. Has one calf and is an herbivore.\n\nFun fact: Despite its zebra-like striped legs, it is the giraffe\'s only living relative, and it was unknown to Western science until it was formally described in 1901.','2026-10-08 10:45:27','2026-10-23 11:34:50'),
(3,'Marbled Cat','Pardofelis marmorata','Felidae','mammal',0,45.0,62.0,28.0,38.0,2.00,5.00,10,12,'near threatened','tropical evergreen forest, secondary growth areas, mixed deciduous forest','Himalayan foothills (Nepal), Southeast Asia mainland, islands of Borneo and Sumatra','Diurnal and solitary. Has one to four kittens and is a carnivore.\n\nFun fact: Its thick, bushy tail is about as long as its head and body, and it uses it as a counterbalance when climbing through the trees.','2026-10-08 11:07:09','2026-10-23 11:34:50'),
(11,'Axolotl','Ambystoma mexicanum','Ambystomatidae','amphibian',0,15.0,45.0,NULL,NULL,0.06,0.25,10,15,'critically endangered','wetlands, canal system','Lake Xochimilco canal system near Mexico City','It is a type of salamander that retains its juvenile stage into adulthood\n\nFun fact: It heals without scarring and can regrow whole lost limbs and its tail, and even rebuild tissue in its eyes and heart.','2026-10-08 13:30:14','2026-10-23 11:42:53'),
(12,'Indian pangolin','Manis crassicaudata','Manidae','mammal',0,84.0,122.0,NULL,NULL,10.00,16.00,NULL,13,'endangered','grasslands, secondary forests, dry/desert regions','Indian subcontinent: Pakistan, India, Nepal, Sri Lanka (possibly extinct in Bangladesh)','It doesnt have any teeth but strong stomach muscles aid in digestion\n\nFun fact: Its skin and scales make up a quarter to a third of its body weight, and it curls into an armoured ball to defend itself, even against tigers.','2026-10-08 13:57:46','2026-10-23 11:42:53'),
(14,'Giant Huntsman spider','Heteropoda maxima','Sparassidae','invertebrate',0,NULL,4.6,NULL,NULL,NULL,NULL,NULL,2,NULL,'caves and cave entrances','Laos','They have the largest legspan of any known spider, up to 30 cm. The Goliath Birdeater is heavier, but has a smaller legspan.\n\nFun fact: Science only described it in 2001, after it was found in a cave in Laos, and females sometimes eat the males after mating.','2026-10-09 06:07:23','2026-10-23 11:42:53'),
(17,'Shoebill','Balaeniceps rex','Balaenicipitidae','bird',0,100.0,140.0,110.0,140.0,4.00,7.00,NULL,35,'vulnerable','freshwater swamps, papyrus and reed beds','tropical East Africa, South Sudan to Zambia','A huge, stork-like wading bird named for its enormous shoe-shaped bill. It hunts lungfish and catfish in dense swamps.\n\nFun fact: It is famously \"statue-like\", standing motionless for long periods before lunging at prey.','2026-10-23 10:20:14','2026-10-23 11:34:50'),
(18,'Pink fairy armadillo','Chlamyphorus truncatus','Chlamyphoridae','mammal',0,9.0,11.5,NULL,NULL,0.12,0.12,NULL,4,'data deficient','sandy plains, dunes, dry scrubland','central Argentina','The smallest armadillo, with silky white fur and a soft pink shell attached to its back only by a thin membrane. It spends most of its life underground.\n\nFun fact: Nicknamed the \"sand-swimmer\", it can bury itself completely in a matter of seconds.','2026-10-23 10:20:14','2026-10-23 10:20:14'),
(19,'Star-nosed mole','Condylura cristata','Talpidae','mammal',0,17.5,20.5,NULL,NULL,0.04,0.08,3,4,'least concern','wetlands, marshes, wet lowland soil','north-eastern North America','A small mole with 22 fleshy tentacles ringing its nose, which it uses as an ultra-sensitive touch organ to find prey.\n\nFun fact: It is the fastest-eating mammal known, identifying and eating a food item in as little as 120 milliseconds.','2026-10-23 10:20:15','2026-10-23 10:20:15'),
(20,'Naked mole-rat','Heterocephalus glaber','Heterocephalidae','mammal',0,8.0,10.0,NULL,NULL,0.03,0.08,NULL,37,'least concern','underground burrows in dry grassland','Horn of Africa (Ethiopia, Kenya, Somalia)','A nearly hairless burrowing rodent that lives in large colonies with a single breeding queen, much like ants or bees.\n\nFun fact: It can live more than 37 years, far longer than any other rodent its size, and is highly resistant to cancer.','2026-10-23 10:20:15','2026-10-23 10:20:15'),
(21,'Aye-aye','Daubentonia madagascariensis','Daubentoniidae','mammal',0,36.0,43.0,NULL,NULL,2.00,2.60,NULL,23,'endangered','rainforest, dry deciduous forest','Madagascar','A nocturnal lemur with ever-growing rodent-like teeth and a long, very thin middle finger.\n\nFun fact: It taps on tree trunks up to eight times per second and listens for hollows, then gnaws a hole and hooks out grubs with its middle finger.','2026-10-23 10:20:15','2026-10-23 10:20:15'),
(22,'Blobfish','Psychrolutes marcidus','Psychrolutidae','fish',0,NULL,30.0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'deep sea floor, 600 to 1,200 m','off south-eastern Australia, Tasmania and New Zealand','A deep-sea fathead sculpin whose loose, gelatinous body looks like an ordinary fish at depth but collapses into a \"blob\" when brought to the surface.\n\nFun fact: Its flesh is slightly less dense than water, so it floats just above the seafloor without spending energy on swimming.','2026-10-23 10:20:15','2026-10-23 10:20:15'),
(23,'Leafy seadragon','Phycodurus eques','Syngnathidae','fish',0,20.0,35.0,NULL,NULL,NULL,NULL,5,10,'least concern','kelp-covered reefs, seagrass beds','southern and western coasts of Australia','A relative of the seahorse covered in leaf-shaped lobes of skin that make it look like drifting seaweed.\n\nFun fact: The leafy lobes are pure camouflage and play no part in swimming, and, as with seahorses, the male carries the eggs on his tail.','2026-10-23 10:20:15','2026-10-23 11:42:53'),
(24,'Fleischmann\'s glass frog','Hyalinobatrachium fleischmanni','Centrolenidae','amphibian',0,1.9,3.2,NULL,NULL,NULL,NULL,NULL,NULL,'least concern','rainforest vegetation near streams','southern Mexico to Ecuador','A tiny lime-green tree frog whose belly skin is translucent, so its internal organs can be seen from below.\n\nFun fact: The male guards the eggs on leaves over streams and even urinates on them to keep them from drying out.','2026-10-23 10:20:15','2026-10-23 10:20:15'),
(25,'Mata mata','Chelus fimbriata','Chelidae','reptile',0,NULL,45.0,NULL,NULL,NULL,17.20,NULL,NULL,NULL,'slow streams, stagnant pools, marshes, swamps','Amazon basin and eastern Guianas, South America','A freshwater turtle with a flat, bark-like shell and a triangular, fringed head that looks like a fallen leaf.\n\nFun fact: It hunts by suddenly opening its huge mouth to create a vacuum that sucks in passing fish, which it swallows whole because it cannot chew.','2026-10-23 10:20:15','2026-10-23 10:20:15'),
(26,'Dumbo octopus','Grimpoteuthis','Opisthoteuthidae','invertebrate',0,20.0,30.0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,'deep sea, 1,000 to 7,000 m','oceans worldwide','A genus of deep-sea umbrella octopuses that swim by flapping a pair of ear-like fins on their mantle.\n\nFun fact: They are named after Disney\'s Dumbo, and are the deepest-living octopuses known, seen as deep as 6,957 m.','2026-10-23 10:20:15','2026-10-23 11:34:50'),
(27,'Greater blue-ringed octopus','Hapalochlaena lunulata','Octopodidae','invertebrate',0,10.0,20.0,NULL,NULL,0.08,0.08,2,2,'least concern','Shallow waters with a mixed seabed','Pacific and Indian Oceans, from Japan to Australia','The greater blue-ringed octopus is one of four species of extremely venomous blue-ringed octopuses belonging to the family Octopodidae. This particular species of blue-ringed octopus is known as one of the most toxic marine animals in the world.\n\nFun fact: The bite is painless to humans but effects appear any time between 15 and 30 minutes and up to four hours, though the rate of onset of symptoms varies by individual, and children are more sensitive to the toxins.','2026-10-23 11:36:03','2026-10-23 12:11:51'),
(28,'Dik-dik','Madoqua','Bovidae','mammal',0,NULL,NULL,NULL,NULL,NULL,NULL,NULL,10,NULL,'Shrublands and savannas','Eastern Africa','The dik-diks are four species of small antelope in the genus Madoqua, all of which live in the bushlands of eastern and southern Africa.\n\nFun fact: Because of their small body size, dik-diks are predicted to have among the highest metabolic rates and highest energy requirement per kilogram of all ruminants.','2026-10-23 13:10:09','2026-10-23 13:10:09');
/*!40000 ALTER TABLE `animals` ENABLE KEYS */;
UNLOCK TABLES;
commit;

--
-- Table structure for table `searches`
--

DROP TABLE IF EXISTS `searches`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!40101 SET character_set_client = utf8mb4 */;
CREATE TABLE `searches` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `query_text` varchar(255) NOT NULL DEFAULT '',
  `animal_id` int(11) DEFAULT NULL,
  `searched_at` timestamp NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `idx_searches_searched_at` (`searched_at`),
  KEY `idx_searches_animal` (`animal_id`),
  CONSTRAINT `fk_searches_animal` FOREIGN KEY (`animal_id`) REFERENCES `animals` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=98 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `searches`
--

LOCK TABLES `searches` WRITE;
/*!40000 ALTER TABLE `searches` DISABLE KEYS */;
set autocommit=0;
INSERT INTO `searches` VALUES
(1,'',NULL,'2026-10-22 09:56:13'),
(2,'',NULL,'2026-10-22 09:56:18'),
(3,'',11,'2026-10-22 09:56:26'),
(4,'',NULL,'2026-10-22 09:56:28'),
(5,'',3,'2026-10-22 12:27:13'),
(6,'',1,'2026-10-22 12:57:30'),
(7,'',2,'2026-10-22 12:57:53'),
(8,'',14,'2026-10-22 12:57:59'),
(9,'',3,'2026-10-22 12:58:13'),
(10,'',11,'2026-10-22 13:11:16'),
(11,'',14,'2026-10-22 13:11:18'),
(12,'',11,'2026-10-23 08:26:10'),
(13,'',2,'2026-10-23 08:26:32'),
(14,'',14,'2026-10-23 08:26:40'),
(15,'',2,'2026-10-23 08:48:48'),
(16,'',11,'2026-10-23 08:48:50'),
(17,'',14,'2026-10-23 08:48:53'),
(18,'',14,'2026-10-23 08:48:54'),
(19,'',11,'2026-10-23 08:49:07'),
(20,'',2,'2026-10-23 09:04:35'),
(21,'',11,'2026-10-23 09:04:40'),
(22,'',2,'2026-10-23 09:04:48'),
(23,'',1,'2026-10-23 09:04:51'),
(24,'',14,'2026-10-23 09:04:54'),
(25,'',12,'2026-10-23 09:04:57'),
(26,'',3,'2026-10-23 09:05:01'),
(27,'',3,'2026-10-23 09:13:22'),
(28,'',11,'2026-10-23 09:33:48'),
(29,'',3,'2026-10-23 09:34:24'),
(30,'',1,'2026-10-23 09:34:32'),
(31,'',1,'2026-10-23 09:34:37'),
(32,'',2,'2026-10-23 09:34:43'),
(33,'',11,'2026-10-23 09:34:51'),
(34,'',3,'2026-10-23 09:38:12'),
(35,'',11,'2026-10-23 09:46:52'),
(36,'',14,'2026-10-23 09:46:56'),
(37,'',11,'2026-10-23 10:10:59'),
(38,'',1,'2026-10-23 10:11:09'),
(39,'',14,'2026-10-23 10:13:23'),
(40,'',12,'2026-10-23 10:13:26'),
(41,'',3,'2026-10-23 10:13:28'),
(42,'',2,'2026-10-23 10:13:30'),
(43,'',1,'2026-10-23 10:13:33'),
(44,'',1,'2026-10-23 10:27:40'),
(45,'',26,'2026-10-23 10:27:47'),
(46,'',22,'2026-10-23 10:27:49'),
(47,'',21,'2026-10-23 10:27:55'),
(48,'',23,'2026-10-23 10:27:59'),
(49,'',24,'2026-10-23 10:28:10'),
(50,'',24,'2026-10-23 10:28:12'),
(51,'',2,'2026-10-23 10:28:16'),
(52,'',14,'2026-10-23 10:42:31'),
(53,'',14,'2026-10-23 10:42:35'),
(54,'',25,'2026-10-23 10:42:54'),
(55,'',20,'2026-10-23 10:43:14'),
(56,'',18,'2026-10-23 10:43:22'),
(57,'',17,'2026-10-23 10:43:28'),
(58,'',24,'2026-10-23 10:43:32'),
(59,'',26,'2026-10-23 10:43:38'),
(60,'',11,'2026-10-23 10:54:51'),
(61,'',2,'2026-10-23 10:55:00'),
(62,'',17,'2026-10-23 10:55:11'),
(63,'',2,'2026-10-23 11:06:21'),
(64,'',23,'2026-10-23 11:06:58'),
(65,'',25,'2026-10-23 11:07:27'),
(66,'',2,'2026-10-23 11:07:31'),
(67,'',17,'2026-10-23 11:07:35'),
(68,'',19,'2026-10-23 11:07:38'),
(69,'',1,'2026-10-23 11:07:48'),
(70,'',22,'2026-10-23 11:23:18'),
(71,'',22,'2026-10-23 11:27:28'),
(72,'',12,'2026-10-23 11:35:29'),
(73,'',24,'2026-10-23 11:35:32'),
(74,'',26,'2026-10-23 11:35:39'),
(75,'',27,'2026-10-23 11:36:06'),
(76,'',27,'2026-10-23 12:10:33'),
(77,'',27,'2026-10-23 12:12:01'),
(78,'',27,'2026-10-23 12:12:13'),
(79,'',26,'2026-10-23 12:12:35'),
(80,'',11,'2026-10-23 13:08:01'),
(81,'',28,'2026-10-23 13:10:17'),
(82,'',2,'2026-10-23 13:10:32'),
(83,'',17,'2026-10-23 13:10:58'),
(84,'',21,'2026-10-23 13:11:22'),
(85,'',26,'2026-10-23 13:12:20'),
(86,'',24,'2026-10-23 13:13:39'),
(87,'',14,'2026-10-23 13:14:13'),
(88,'',23,'2026-10-23 13:14:49'),
(89,'',3,'2026-10-23 13:15:19'),
(90,'',25,'2026-10-23 13:17:07'),
(91,'',20,'2026-10-23 13:17:40'),
(92,'',2,'2026-10-23 13:18:29'),
(93,'',18,'2026-10-23 13:19:04'),
(94,'',17,'2026-10-23 13:19:25'),
(95,'',19,'2026-10-23 13:20:02'),
(96,'',1,'2026-10-23 13:21:02'),
(97,'blo',22,'2026-10-23 13:22:35');
/*!40000 ALTER TABLE `searches` ENABLE KEYS */;
UNLOCK TABLES;
commit;

--
-- Dumping events for database 'animaldatabase'
--

--
-- Dumping routines for database 'animaldatabase'
--
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*M!100616 SET NOTE_VERBOSITY=@OLD_NOTE_VERBOSITY */;

-- Dump completed
