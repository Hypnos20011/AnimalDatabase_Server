SET NAMES utf8mb4;
START TRANSACTION;
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Axolotl', 'Feuchtgebiete, Kanalsystem', 'Kanalsystem des Xochimilco-Sees bei Mexiko-Stadt', 'Der Axolotl ist eine Art aus der Familie der Querzahnmolche. Er ist neoten, wird also geschlechtsreif, ohne eine Metamorphose zu durchlaufen, und auch die erwachsenen Tiere leben vollständig im Wasser und tragen gut sichtbare äußere Kiemen.

Fun fact: Anders als die meisten Schwanzlurcharten behalten Axolotl ihre äußeren Kiemen, auch wenn sie erwachsen werden.' FROM animals WHERE scientific_name = 'Ambystoma mexicanum'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Fingertier', 'Regenwald oder trockener Laubwald', 'Ostküste Madagaskars', 'Das Fingertier, auch Aye-Aye genannt, ist ein Lemur mit langen Fingern und gehört zu den Feuchtnasenprimaten Madagaskars. Es hat nagetierähnliche, ständig nachwachsende Zähne und einen besonders dünnen Mittelfinger, mit dem es Maden und Larven aus Baumstämmen herausholt.

Fun fact: Das Fingertier gilt als vermutlich einziger Primat, der seine Beute mithilfe von Echoortung aufspürt.' FROM animals WHERE scientific_name = 'Daubentonia madagascariensis'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Zebramanguste', 'Savannen, lichte Wälder und Grasland', 'Große Teile des östlichen, südöstlichen und südlichen Zentralafrikas', 'Die Zebramanguste ist eine Mangustenart, die vom Sahel bis ins südliche Afrika vorkommt. Sie bewohnt Savannen, lichte Wälder und Grasland und ernährt sich vor allem von Käfern und Tausendfüßern.

Fun fact: Anders als bei den meisten anderen gesellig lebenden Mangustenarten können sich in einer Gruppe von Zebramangusten alle Weibchen fortpflanzen.' FROM animals WHERE scientific_name = 'Mungos mungo'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Löffelhund', 'Kurzgrassavannen und trockenes Grasland', 'Südliches Afrika', 'Der Löffelhund ist ein Fuchs, der in der afrikanischen Savanne lebt. Er ist die einzige heute lebende Art der Gattung Otocyon und die Schwesterart aller übrigen Vertreter der Tribus Vulpini; genetisch ist er also kein echter Fuchs.

Fun fact: Der Löffelhund ist der einzige echte Insektenfresser unter den Hunden und bevorzugt deutlich Erntetermiten (Hodotermes mossambicus), die 80–90 % seiner Nahrung ausmachen können.' FROM animals WHERE scientific_name = 'Otocyon megalotis'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Binturong', 'Vorgebirge oder bewaldete Hügel', 'Süd- und Südostasien', 'Der Binturong oder Marderbär ist eine baumbewohnende Schleichkatze aus Süd- und Südostasien. Er ist die einzige Art der Gattung Arctictis und wird in neun Unterarten gegliedert.

Fun fact: Sein Körperbau ist einzigartig: Er besitzt Moschusdrüsen, die nach Popcorn riechen, und einen langen, buschigen Greifschwanz.' FROM animals WHERE scientific_name = 'Arctictis binturong'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Blobfisch', 'Tiefe Gewässer', 'Küsten des australischen Festlands und Tasmaniens', 'Psychrolutes marcidus, auch Glattkopf-Blobfisch oder einfach Blobfisch genannt, ist ein Tiefseefisch aus der Familie der Dickkopf-Groppen (Psychrolutidae). Er lebt in den tiefen Gewässern vor den Küsten des australischen Festlands und Tasmaniens sowie in den Gewässern Neuseelands.

Fun Fact: Das Fleisch des Blobfischs besteht hauptsächlich aus einer gallertartigen Masse mit sehr feinen, weichen Knochen. So übersteht der Fisch den hohen Druck und kann in großen Tiefen knapp über dem Meeresboden schweben, ohne viel Energie aufzuwenden.' FROM animals WHERE scientific_name = 'Psychrolutes marcidus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Nebelparder', 'Dichter, immergrüner tropischer Primärwald, vom Vorgebirge bis etwa 1450 m', 'Vom Himalaya über Nordostindien und Bhutan bis zum südostasiatischen Festland und nach Südchina', 'Der Nebelparder, auch Festland-Nebelparder genannt, ist eine Wildkatze mit großen, dunkelgrauen Flecken sowie unregelmäßigen Tupfen und Streifen, die an Wolken erinnern. Die Kopf-Rumpf-Länge beträgt 68,6 bis 108 cm, dazu kommt ein 61 bis 91 cm langer Schwanz, der beim Klettern in Bäumen dem Gleichgewicht dient.

Fun fact: Der Nebelparder wird oft als „moderne Säbelzahnkatze“ bezeichnet, weil er im Verhältnis zu seiner Körpergröße die längsten Eckzähne hat.' FROM animals WHERE scientific_name = 'Neofelis nebulosa'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Dumbo-Oktopus', 'Tiefsee, 1000 bis 7000 m', 'Weltmeere', 'Eine Tiefseekraken-Gattung aus der Familie der Scheibenschirme, die sich durch Schlagen zweier ohrenartiger Flossen an ihrem Mantel fortbewegen.

Fun fact: Sie sind nach Disneys Dumbo benannt und die am tiefsten lebenden bekannten Kraken; beobachtet wurden sie noch in 6957 m Tiefe.' FROM animals WHERE scientific_name = 'Grimpoteuthis'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Bennettkasuar', 'Bergnebelwälder und tropische Regenwälder', 'Insel Neuguinea', 'Der Bennettkasuar, auch Zwergkasuar, Kleiner Kasuar, Bergkasuar oder Muruk genannt, ist die kleinste der drei heute lebenden Kasuararten. Wie alle Kasuare ist er ein großer, überwiegend schwarzer, flugunfähiger Vogel.

Fun fact: Der Helm des Bennettkasuars ist meist tiefschwarz, und anders als bei Helm- und Einlappenkasuar leuchtet er unter UV-Licht nicht.' FROM animals WHERE scientific_name = 'Casuarius bennetti'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Meeraal', 'Felsküsten und Meeresgrund, bis etwa 1000 m Tiefe', 'Nordostatlantik (Norwegen und Island bis Senegal), Mittelmeer und Schwarzes Meer', 'Der Meeraal ist eine Art aus der Familie der Meeraale (Congridae). Er ist der schwerste Aal der Welt und im Nordostatlantik einschließlich des Mittelmeers heimisch.

Fun fact: Die größten Muränenarten können ihm in der Länge gleichkommen oder ihn knapp übertreffen, sind aber meist schlanker und daher leichter als große Meeraale.' FROM animals WHERE scientific_name = 'Conger conger'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Tigermuräne', 'Felsige, spaltenreiche Böden', 'Wärmere Teile des östlichen Atlantiks', 'Die Tigermuräne, im Englischen auch als „Fangtooth moray“ oder „Bird-eye conger“ bekannt, ist ein Aalfisch aus der Familie der Muränen (Muraenidae).' FROM animals WHERE scientific_name = 'Enchelycore anatina'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Fennek', 'Sanddünen und Sandwüste mit spärlicher Vegetation', 'Sahara, von Marokko und Mauretanien bis in den Nordsudan, nach Ägypten und auf den Sinai', 'Der Fennek ist ein kleiner Fuchs aus den Wüsten Nordafrikas, dessen Verbreitung von der Westsahara und Mauretanien bis zur Sinai-Halbinsel reicht. Sein auffälligstes Merkmal sind die ungewöhnlich großen Ohren, mit denen er Wärme abgibt und Beutetiere unter der Erde hört.

Fun fact: Das älteste Fennek-Männchen in Menschenobhut wurde 14 Jahre alt, das älteste Weibchen 13 Jahre.' FROM animals WHERE scientific_name = 'Vulpes zerda'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Fleischmann-Glasfrosch', 'Regenwaldvegetation an Bächen', 'Südmexiko bis Ecuador', 'Ein winziger, limettengrüner Baumfrosch mit durchscheinender Bauchhaut, durch die man von unten seine inneren Organe sehen kann.

Fun fact: Das Männchen bewacht die Eier auf Blättern über Bächen und uriniert sogar auf sie, damit sie nicht austrocknen.' FROM animals WHERE scientific_name = 'Hyalinobatrachium fleischmanni'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Gangesgavial', 'Sandige und feuchte Ufer', NULL, 'Der Gangesgavial, auch Gavial oder Gharial genannt und als fischfressendes Krokodil bekannt, gehört zur Familie der Gaviale (Gavialidae) und zählt zu den längsten heute lebenden Krokodilen. Ausgewachsene Weibchen messen 2,6–4,5 m, Männchen 3–6 m.

Fun fact: Die ältesten bekannten Darstellungen des Gangesgavials sind etwa 4000 Jahre alt und stammen aus dem Industal.' FROM animals WHERE scientific_name = 'Gavialis gangeticus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Riesenschnurfüßer', 'Wälder', 'Ostafrika (Mosambik bis Kenia) und Südarabien (Dhofar)', 'Archispirostreptus gigas, bekannt als Afrikanischer Riesentausendfüßer, Shongololo oder Bongololo, ist die größte heute lebende Tausendfüßerart. Er wird bis zu 33,5 Zentimeter lang und erreicht einen Umfang von 67 Millimetern.

Fun fact: Er hat etwa 256 Beine; da sich die Zahl der Beine mit jeder Häutung ändert, kann sie von Tier zu Tier verschieden sein.' FROM animals WHERE scientific_name = 'Archispirostreptus gigas'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Großer Ameisenbär', NULL, 'Mittel- und Südamerika', 'Der Große Ameisenbär ist ein großes, insektenfressendes Säugetier aus Mittel- und Südamerika. Er ist die größte der vier heute lebenden Ameisenbärenarten, die zusammen mit den Faultieren zur Ordnung der Zahnarmen (Pilosa) gestellt werden.

Fun fact: Anders als die Vorderfüße tragen die Hinterfüße an allen fünf Zehen kurze Krallen und setzen beim Gehen mit der ganzen Sohle auf (Sohlengänger).' FROM animals WHERE scientific_name = 'Myrmecophaga tridactyla'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Riesen-Zitterrochen', 'Flache Sandböden, oft in Riffnähe', 'Ostpazifik vom Golf von Kalifornien bis Panama', 'Narcine entemedor, im Englischen „Giant electric ray“ oder „Cortez electric ray“ genannt, ist ein Rochen aus der Familie Narcinidae, der im Ostpazifik vom Golf von Kalifornien bis Panama vorkommt. Er lebt im Flachwasser auf sandigem Grund und manchmal in der Nähe von Riffen.' FROM animals WHERE scientific_name = 'Narcine entemedor'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Goldkronen-Flughund', NULL, 'Philippinen', 'Der Goldkronen-Flughund ist eine Flughundart, die nur auf den Philippinen vorkommt. Seit seiner Erstbeschreibung im Jahr 1831 wurden drei Unterarten anerkannt, von denen eine ausgestorben ist.

Fun fact: Mit bis zu 1,4 kg Gewicht gehört er zu den größten Fledertieren der Welt – nur der Indische Riesenflughund und der Große Flughund können schwerer werden.' FROM animals WHERE scientific_name = 'Acerodon jubatus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Laotische Riesenkrabbenspinne', 'Höhlen und Höhleneingänge', 'Laos', 'Die Laotische Riesenkrabbenspinne ist eine Art aus der Familie der Riesenkrabbenspinnen (Sparassidae) und lebt in Laos. Gemessen an der Beinspannweite, die bis zu 30 cm erreichen kann, gilt sie als größte Spinne der Welt.' FROM animals WHERE scientific_name = 'Heteropoda maxima'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Schrecklicher Pfeilgiftfrosch', 'Feuchte Wälder', 'Pazifikküste Kolumbiens in den Departamentos Cauca und Valle del Cauca im Chocó-Regenwald', 'Der Schreckliche Pfeilgiftfrosch, auch Goldener Pfeilgiftfrosch genannt, ist ein Baumsteigerfrosch, der nur in den Regenwäldern Kolumbiens vorkommt. Weil sein ohnehin kleines Verbreitungsgebiet zerstört wird, ist er inzwischen stark gefährdet.

Fun fact: Trotz seiner geringen Größe gilt dieser Frosch als das giftigste heute lebende Tier der Erde.' FROM animals WHERE scientific_name = 'Phyllobates terribilis'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Großer Blaugeringelter Krake', 'Flachwasser mit gemischtem Untergrund', 'Pazifik und Indischer Ozean, von Japan bis Australien', 'Der Große Blaugeringelte Krake ist eine von vier Arten der hochgiftigen Blaugeringelten Kraken aus der Familie der Echten Kraken (Octopodidae). Diese Art gilt als eines der giftigsten Meerestiere der Welt.

Fun fact: Der Biss ist für Menschen schmerzlos, doch die Wirkung setzt irgendwann zwischen 15 und 30 Minuten und bis zu vier Stunden danach ein. Wie schnell Symptome auftreten, ist individuell verschieden, und Kinder reagieren empfindlicher auf das Gift.' FROM animals WHERE scientific_name = 'Hapalochlaena lunulata'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Günther-Dikdik', 'Dornbuschdickichte, Savannengrasland und Galeriewälder', 'Äthiopien, Kenia, Somalia, der Südosten des Südsudans und der Nordosten Ugandas', 'Das Günther-Dikdik ist eine kleine Antilope aus den Trockengebieten Ostafrikas.

Fun fact: Das Günther-Dikdik zählt zu den kleinsten Huftieren und wiegt ausgewachsen nur 3–5 Kilogramm.' FROM animals WHERE scientific_name = 'Madoqua guentheri'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Harpyie', 'Tropische Tieflandregenwälder in der oberen Kronenschicht', NULL, 'Die Harpyie ist eine große neotropische Adlerart. Sie wird auch Amerikanische Harpyie genannt, um sie vom Papuaadler zu unterscheiden, der mitunter als Neuguinea-Harpyie bezeichnet wird.

Fun fact: Mit 36 Tagen kann das Küken stehen und unbeholfen laufen.' FROM animals WHERE scientific_name = 'Harpia harpyja'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Humboldt-Kalmar', 'Pelagisch, etwa 200 bis 700 m tief', 'Ostpazifik, von Feuerland bis Kalifornien', 'Der Humboldt-Kalmar, auch Roter Teufel, Riesenkalmar oder Jumbo-Kalmar genannt, ist ein großer, räuberischer Kalmar aus dem östlichen Pazifik. Er ist die einzige bekannte Art der Gattung Dosidicus innerhalb der Unterfamilie Ommastrephinae.

Fun fact: Eine Untersuchung des Mageninhalts von über 2000 Kalmaren, die außerhalb der ausschließlichen Wirtschaftszone vor Chile gefangen wurden, ergab, dass Kannibalismus vermutlich die wichtigste Nahrungsquelle war.' FROM animals WHERE scientific_name = 'Dosidicus gigas'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Königsriesenhörnchen', 'Tropischer Laub-, halbimmergrüner und feuchter immergrüner Wald', 'Indien', 'Das Königsriesenhörnchen, auch Indisches Riesenhörnchen oder Malabar-Riesenhörnchen genannt, ist ein großes, mehrfarbiges Baumhörnchen, das nur in den Wäldern Indiens vorkommt. Es ist tagaktiv, lebt auf Bäumen und ernährt sich überwiegend pflanzlich.

Fun fact: Es darf nicht mit echten Albinos verwechselt werden, die beim Königsriesenhörnchen selten nachgewiesen sind; diese sind weißer und haben im Gegensatz zu R. i. dealbata rosafarbene Augen.' FROM animals WHERE scientific_name = 'Ratufa indica'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Vorderindisches Schuppentier', 'Grasland und Sekundärwälder', 'Indischer Subkontinent', 'Das Vorderindische Schuppentier, auch Dickschwanz-Schuppentier genannt, ist ein Schuppentier des indischen Subkontinents. Wie andere Schuppentiere trägt es große, sich überlappende Schuppen am Körper, die als Panzer dienen.

Fun fact: Das auffälligste Merkmal des Schuppentiers ist sein massiver Schuppenpanzer, der die Oberseite des Gesichts und den ganzen Körper bedeckt, mit Ausnahme des Bauchs und der Innenseiten der Beine.' FROM animals WHERE scientific_name = 'Manis crassicaudata'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Indischer roter Skorpion', 'Buschland, steinige Hügel, Ackerland und Plantagen', 'Großteil Indiens, Ostpakistan und das östliche Tiefland Nepals, neuerdings auch Sri Lanka', 'Hottentotta tamulus, der Indische Rote Skorpion, auch Ostindischer Skorpion genannt, ist eine Skorpionart aus der Familie der Buthidae. Er kommt im Großteil Indiens, im Osten Pakistans und im östlichen Tiefland Nepals vor und wurde neuerdings auch auf Sri Lanka nachgewiesen.

Fun fact: Mit einem Anteil von 48,43 % war er mit Abstand die häufigste der sechs in dieser Studie erfassten Skorpionarten.' FROM animals WHERE scientific_name = 'Hottentotta tamulus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Japanische Riesenkrabbe', 'Spalten und Löcher in den tieferen Bereichen des Meeres', 'Präfektur Iwate und vor Su-ao in Taiwan', 'Die Japanische Riesenkrabbe ist eine Meereskrabbe und die größte Krabbe in den Gewässern um Japan. Mit einer Spannweite von etwa 3,75 Metern hat sie die größte Beinspannweite aller Gliederfüßer.

Fun fact: Die Japanische Riesenkrabbe häutet sich auf besondere Weise: Der Vorgang dauert etwa 100 Minuten, in denen sie sich nicht bewegen kann; er beginnt am hinteren Teil des Panzers und endet mit den Laufbeinen.' FROM animals WHERE scientific_name = 'Macrocheira kaempferi'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Komodowaran', 'Tropische Savanne, trockenes offenes Grasland und Waldränder', 'Indonesische Inseln Komodo, Rinca, Flores, Gili Dasami und Gili Motang', 'Der Komodowaran, auch Komododrache genannt, ist ein großes Reptil aus der Familie der Warane (Varanidae), das nur auf den indonesischen Inseln Komodo, Rinca, Flores, Gili Dasami und Gili Motang vorkommt. Die größte heutige Population lebt im Komodo-Nationalpark im Osten Indonesiens.

Fun fact: Bei günstigem Wind und dank seiner Gewohnheit, beim Gehen den Kopf hin und her zu schwenken, kann ein Komodowaran Aas möglicherweise aus 4–9,5 km Entfernung wittern.' FROM animals WHERE common_name = 'Komodo dragon'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Großer Fetzenfisch', 'Mit Kelp bewachsene Riffe, Seegraswiesen', 'Süd- und Westküste Australiens', 'Ein Verwandter des Seepferdchens, dessen Körper mit blattförmigen Hautlappen bedeckt ist, sodass er wie treibender Tang aussieht.

Fun fact: Die blattartigen Lappen dienen nur der Tarnung und spielen beim Schwimmen keine Rolle; wie bei Seepferdchen trägt das Männchen die Eier am Schwanz.' FROM animals WHERE scientific_name = 'Phycodurus eques'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Marmorkatze', 'Feuchte und gemischte laubabwerfend-immergrüne tropische Wälder', 'Östliches Himalaya-Vorland und tropisches Indomalaya ostwärts bis Südwestchina', 'Die Marmorkatze ist eine kleine Wildkatze, die vom östlichen Himalaya bis nach Südostasien verbreitet ist und dort Wälder bis in 2500 m Höhe bewohnt. Da sie ein großes Verbreitungsgebiet hat, wird sie seit 2015 auf der Roten Liste der IUCN als potenziell gefährdet geführt.' FROM animals WHERE scientific_name = 'Pardofelis marmorata'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Fransenschildkröte', 'langsam fließende Bäche, stehende Tümpel, Marschen, Sümpfe', 'Amazonasbecken und östliches Guayana, Südamerika', 'Eine Süßwasserschildkröte mit flachem, rindenartigem Panzer und einem dreieckigen, mit Hautfransen besetzten Kopf, der wie ein abgefallenes Blatt aussieht.

Fun fact: Sie jagt, indem sie ihr riesiges Maul plötzlich aufreißt und so einen Sog erzeugt, der vorbeischwimmende Fische einsaugt; diese schluckt sie im Ganzen, da sie nicht kauen kann.' FROM animals WHERE scientific_name = 'Chelus fimbriata'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Nacktmull', 'unterirdische Baue in trockenem Grasland', 'Horn von Afrika (Äthiopien, Kenia, Somalia)', 'Ein fast haarloses, grabendes Nagetier, das ähnlich wie Ameisen oder Bienen in großen Kolonien mit einer einzigen fortpflanzungsfähigen Königin lebt.

Fun fact: Er kann über 37 Jahre alt werden, weit älter als jedes andere Nagetier seiner Größe, und ist äußerst widerstandsfähig gegen Krebs.' FROM animals WHERE scientific_name = 'Heterocephalus glaber'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Namibgecko', 'Sandige Wüstengebiete', 'Namib-Wüste im südwestlichen Afrika', 'Pachydactylus rangei, der Namibgecko oder Namib-Schwimmfußgecko, ist eine kleine Echse aus der Familie der Geckos (Gekkonidae). Er bewohnt die Trockengebiete Angolas, Namibias und Südafrikas und wurde 1908 vom schwedischen Zoologen Lars Gabriel Andersson beschrieben, der ihn nach seinem Entdecker, dem deutschen Geologen Dr. Paul Range, benannte.

Fun fact: Die Haut ist mit feinen, glatten Schuppen bedeckt und durchscheinend, sodass man einige innere Organe hindurch erkennen kann.' FROM animals WHERE scientific_name = 'Pachydactylus rangei'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Neuweltliches Kleinsthörnchen', 'Tropische Tieflandregenwälder', 'Nördliches Südamerika (Französisch-Guayana, Suriname, Zentralbrasilien, Nordperu, Südkolumbien)', 'Das Neuweltliche Kleinsthörnchen ist ein südamerikanisches Baumhörnchen und die einzige lebende Art der Gattung Sciurillus und der Unterfamilie Sciurillinae. Genetische Untersuchungen haben gezeigt, dass es die Schwestergruppe aller übrigen Hörnchen bildet.

Fun fact: Das Neuweltliche Kleinsthörnchen ist die kleinste in Amerika heimische Baumhörnchenart.' FROM animals WHERE scientific_name = 'Sciurillus pusillus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Laucharassari', 'Feuchter Bergwald', 'Bergregionen von Südmexiko bis Südpanama', 'Der Laucharassari, auch Waglers Arassari genannt, ist eine Vogelart aus der Familie der Tukane (Ramphastidae). Er kommt in Bergregionen von Südmexiko bis Südpanama vor.' FROM animals WHERE scientific_name = 'Aulacorhynchus prasinus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Nördlicher Tamandua', NULL, 'Tropische und subtropische Wälder von Südmexiko durch Mittelamerika', 'Der Nördliche Tamandua ist eine Tamandua-Art, also ein Ameisenbär aus der Familie Myrmecophagidae.

Fun fact: Er kann bis zu 9000 Insekten am Tag aus 50 bis 80 verschiedenen Nestern fressen, die er am Geruch erkennt und dann mit seinen kräftigen Krallen aufgräbt.' FROM animals WHERE scientific_name = 'Tamandua mexicana'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Okapi', 'Kronenwald', 'Demokratische Republik Kongo', 'Das Okapi, auch Waldgiraffe oder Zebragiraffe genannt, ist ein Paarhufer, der nur im Nordosten der Demokratischen Republik Kongo vorkommt. Es ist die einzige Art der Gattung Okapia.

Fun fact: Im Ituri-Wald sind Okapis einzigartig, denn sie sind das einzige bekannte Säugetier, das sich ausschließlich von Pflanzen des Unterwuchses ernährt; mit ihrer 46 cm langen Zunge wählen sie gezielt geeignete Pflanzen aus.' FROM animals WHERE scientific_name = 'Okapia johnstoni'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Schmuck-Geisterpfeifenfisch', 'Strömungsreiche Riffkanten, zwischen Gorgonien und Haarsternen', 'Indowestpazifik, vom Roten Meer bis Tonga', 'Solenostomus paradoxus, der Schmuck-Geisterpfeifenfisch oder Harlekin-Geisterpfeifenfisch, ist ein Geisterpfeifenfisch aus der Familie Solenostomidae. Der Artname leitet sich vom griechischen paradoxos ab und bezieht sich auf das ungewöhnliche Aussehen des Fisches.

Fun fact: Doch anders als bei ihren Verwandten entwickeln sich die Embryonen von Solenostomus am Weibchen.' FROM animals WHERE scientific_name = 'Solenostomus paradoxus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Schmuckbaumnatter', 'Feuchtwald, baumbewohnend', 'Thailand, Indonesien, Brunei, Indien, Malaysia, Myanmar, Philippinen und Singapur', 'Die Schmuckbaumnatter, auch Paradies-Flugschlange genannt, ist eine Natternart aus Südostasien. Wie alle Arten der Gattung Chrysopelea kann sie gleiten, indem sie ihren Körper mithilfe der Rippen zu einem flachen Band ausbreitet.

Fun fact: Die Flugschlange bewegt sich auf eine einzigartige Weise, die sich von anderen gleitenden oder fliegenden Tieren unterscheidet, denn sie ist walzenförmig und hat keine Gliedmaßen wie Beine oder Flügel.' FROM animals WHERE scientific_name = 'Chrysopelea paradisi'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Brasilianischer Riesenläufer', 'Tropischer Regenwald und Trockenwald; Laubstreu, unter Steinen und Totholz', 'Nördliches Südamerika', 'Scolopendra gigantea, auch als Peruanischer Riesenläufer oder Amazonas-Riesenhundertfüßer bekannt, ist ein Hundertfüßer der Gattung Scolopendra. Mit einer Länge von über 30 Zentimetern ist er der größte Hundertfüßer der Welt.

Fun fact: Von großen Exemplaren von S. gigantea ist bekannt, dass sie mit besonderen Strategien und viel Muskelkraft Fledermäuse fangen.' FROM animals WHERE scientific_name = 'Scolopendra gigantea'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Gürtelmull', 'sandige Ebenen, Dünen, trockenes Buschland', 'Zentralargentinien', 'Das kleinste Gürteltier, mit seidigem weißem Fell und einem weichen, rosafarbenen Panzer, der nur über eine dünne Membran mit dem Rücken verbunden ist. Es verbringt den größten Teil seines Lebens unter der Erde.

Fun fact: Sein Spitzname lautet „Sandschwimmer“, denn es kann sich binnen Sekunden vollständig eingraben.' FROM animals WHERE scientific_name = 'Chlamyphorus truncatus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Schnabeltier', 'Süßwasserflüsse, Bäche und Seen mit erdigen Ufern für Baue', 'Süßgewässer Ostaustraliens', 'Das Schnabeltier ist ein teilweise im Wasser lebendes, eierlegendes Säugetier, das nur im Osten Australiens einschließlich Tasmaniens vorkommt. Es ist der einzige lebende Vertreter der Familie Ornithorhynchidae und der Gattung Ornithorhynchus, auch wenn einige verwandte Arten fossil überliefert sind.

Fun fact: Kloakentiere sind die einzigen Säugetiere (abgesehen vom Guyana-Delfin), von denen bekannt ist, dass sie elektrische Felder wahrnehmen können.' FROM animals WHERE scientific_name = 'Ornithorhynchus anatinus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Ligusterschwärmer', 'Waldränder, Hecken, Parks und Gärten mit Liguster und Flieder', 'Großteil der Paläarktis', 'Sphinx ligustri, der Ligusterschwärmer, ist ein Nachtfalter, der in weiten Teilen der Paläarktis vorkommt. Carl von Linné beschrieb die Art erstmals 1758 in der 10. Auflage seines Werks Systema Naturae.' FROM animals WHERE scientific_name = 'Sphinx ligustri'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Rot-Weißes Riesengleithörnchen', 'Bergwald, 800 bis 3750 m', 'Zentral- und Südchina sowie Taiwan', 'Das Rot-Weiße Riesengleithörnchen ist eine Nagetierart aus der Familie der Hörnchen (Sciuridae).

Fun fact: Das Rot-Weiße Riesengleithörnchen wurde als größtes Gleithörnchen bezeichnet, obwohl einige andere Arten zumindest in manchen Körpermaßen mit ihm gleichziehen.' FROM animals WHERE scientific_name = 'Petaurista alborufus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Robuster Geisterpfeifenfisch', 'Küstenriffe und algenreiche Bereiche, 2 bis 25 m', 'Rotes Meer und tropischer Indopazifik', 'Der Robuste Geisterpfeifenfisch, auch Blauflossen-Geisterpfeifenfisch, Raceks Geisterpfeifenfisch, Dickschnauzen-Geisterpfeifenfisch oder Rundschwanz-Geisterpfeifenfisch genannt, ist ein Geisterpfeifenfisch aus der Familie Solenostomidae.

Fun fact: Der Robuste Geisterpfeifenfisch wird bis zu 17 cm lang und ist damit der größte aller Geisterpfeifenfische.' FROM animals WHERE scientific_name = 'Solenostomus cyanopterus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Schuhschnabel', 'Süßwassersümpfe, Papyrus- und Schilfbestände', 'Tropisches Ostafrika, vom Südsudan bis Sambia', 'Ein riesiger, storchenähnlicher Schreitvogel, der nach seinem enormen, schuhförmigen Schnabel benannt ist. Er jagt in dichten Sümpfen Lungenfische und Welse.

Fun fact: Berühmt ist er für seine „statuenhafte“ Haltung: Lange steht er reglos da, bevor er blitzschnell nach der Beute stößt.' FROM animals WHERE scientific_name = 'Balaeniceps rex'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Kurzschnabeligel', 'Fast alle Lebensräume Australiens, von Wäldern bis zu Wüsten', NULL, 'Der Kurzschnabeligel, auch Gewöhnlicher Ameisenigel oder Kurznasenigel genannt, ist eine von vier lebenden Ameisenigelarten und der einzige Vertreter der Gattung Tachyglossus. Der Name kommt aus dem Altgriechischen: ταχύς bedeutet „schnell“ und γλῶσσα „Zunge“.

Fun fact: Ameisenigel können elektrische Felder von 1,8 mV/cm wahrnehmen – 1000-mal empfindlicher als der Mensch – und graben sogar vergrabene Batterien aus.' FROM animals WHERE scientific_name = 'Tachyglossus aculeatus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Zwergameisenbären', 'Verschiedene Waldtypen', 'Nördlicher Atlantischer Regenwald im Osten Brasiliens', 'Der Zwergameisenbär bewegt sich langsam und ernährt sich hauptsächlich von Ameisen, von denen er 700 bis 5000 am Tag frisst. Zwergameisenbären fressen außerdem Wespen und deren Puppen; sie überfallen die Wespennester nachts, wenn die Wespen träge sind und sich nicht wehren können.

Fun fact: Weil er den Samenfasern dieser Bäume ähnelt, kann er sich in ihnen tarnen und so Angriffen von Greifvögeln wie Habichten und vor allem Harpyien entgehen.' FROM animals WHERE scientific_name = 'Cyclopes'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Helmkasuar', 'Tropische Regenwälder', 'Indonesien, Papua-Neuguinea und Nordostaustralien', 'Der Helmkasuar, auch Doppellappenkasuar oder Australischer Kasuar genannt, ist ein großer, flugunfähiger, überwiegend schwarzer Vogel aus Indonesien, Papua-Neuguinea und dem Nordosten Australiens. Er ist eine der drei lebenden Kasuararten, neben dem Bennettkasuar und dem Einlappenkasuar.

Fun fact: Helmkasuare gehören zu den größten lebenden Vögeln und sind im Durchschnitt nur leichter als Strauße; die größten heute lebenden Exemplare wogen etwa 85 kg und waren 2 m hoch.' FROM animals WHERE scientific_name = 'Casuarius casuarius'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Südlicher Tamandua', 'Feucht- und Trockenwälder', 'Südamerika', 'Der Südliche Tamandua, auch Kleiner Ameisenbär genannt, ist eine Ameisenbärenart aus Südamerika und von der Karibikinsel Trinidad. Er lebt als Einzelgänger in vielen Lebensräumen, von alten Wäldern über stark gestörte Sekundärwälder bis zu trockenen Savannen.' FROM animals WHERE scientific_name = 'Tamandua tetradactyla'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Sternmull', 'Feuchtgebiete, Marschen, nasse Tieflandböden', 'nordöstliches Nordamerika', 'Ein kleiner Maulwurf mit 22 fleischigen Tentakeln rund um die Nase, die ihm als hochempfindliches Tastorgan bei der Beutesuche dienen.

Fun fact: Er ist das am schnellsten fressende bekannte Säugetier und kann ein Beutetier in nur 120 Millisekunden erkennen und verzehren.' FROM animals WHERE scientific_name = 'Condylura cristata'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Malaien-Gleitflieger', 'Tropischer Regenwald, Plantagen und Gärten', 'Südostasien', 'Der Malaien-Gleitflieger, auch Sunda-Gleitflieger oder Malaiischer Colugo genannt, ist die einzige Riesengleiterart der Gattung Galeopterus. Er kommt in Südostasien von Südmyanmar, Thailand, Südvietnam und Malaysia bis Singapur und Indonesien vor und wird auf der Roten Liste der IUCN als nicht gefährdet eingestuft.

Fun fact: Die stärksten Landekräfte treten nach kurzen Gleitflügen auf; längere Gleitflüge enden sanfter, weil er seinen Flug aerodynamisch abbremsen kann.' FROM animals WHERE scientific_name = 'Galeopterus variegatus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Sunda-Plumplori', 'Tropischer Tieflandregenwald und Waldränder', 'Tropische Regenwälder mit geschlossenem Kronendach', 'Der Sunda-Plumplori, auch Großer Plumplori genannt, ist ein Feuchtnasenprimat aus der Gattung der Plumploris, der in Indonesien, Westmalaysia, Südthailand und Singapur vorkommt.

Fun fact: Die meiste Zeit verbringt er mit dem Fressen von Phloemsaft (34,9 %), Blütennektar und nektarliefernden Pflanzenteilen (31,7 %) sowie Früchten (22,5 %).' FROM animals WHERE scientific_name = 'Nycticebus coucang'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Beutelteufel', NULL, 'Insel Tasmanien', 'Der Beutelteufel ist ein fleischfressendes Beuteltier aus der Familie der Raubbeutler (Dasyuridae). Früher war er auf dem gesamten australischen Festland verbreitet, starb dort aber vor etwa 3500 Jahren aus; heute lebt er nur noch auf der Insel Tasmanien.

Fun fact: Anders als die meisten anderen Raubbeutler kann der Beutelteufel seine Körpertemperatur gut regulieren und ist auch um die Mittagszeit aktiv, ohne zu überhitzen.' FROM animals WHERE scientific_name = 'Sarcophilus harrisii'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Mangrovenqualle', NULL, 'Wärmere Küstenregionen weltweit', 'Cassiopea ist eine Gattung echter Quallen aus der Familie Cassiopeidae. Sie kommen in wärmeren Küstenregionen auf der ganzen Welt vor, etwa in flachen Mangrovensümpfen, Wattflächen, Kanälen, flachen Lagunen und Buchten sowie Seegraswiesen in Florida, der Karibik, Hawaii, dem Roten Meer, dem Mittelmeer und Mikronesien.

Fun fact: Anders als die meisten anderen Quallen schwimmt die Mangrovenqualle nur selten und verbringt den Großteil ihres Lebens mit dem Schirm auf dem Boden und der Unterseite nach oben.' FROM animals WHERE scientific_name = 'Cassiopea'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Weißrüssel-Nasenbär', NULL, NULL, 'Der Weißrüssel-Nasenbär, auch Coatimundi genannt, ist eine Nasenbärenart aus der Familie der Kleinbären (Procyonidae). Je nach Region wird er im Spanischen unter anderem antoon, gato solo, pizote oder tejón genannt.

Fun fact: Anders als viele ihrer Verwandten aus der Familie der Kleinbären, etwa Waschbären und Wickelbären, sind Nasenbären tagaktiv und suchen den Großteil ihrer Nahrung tagsüber.' FROM animals WHERE scientific_name = 'Nasua narica'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
INSERT INTO animal_translations (animal_id, language, common_name, habitat, range_region, profile)
SELECT id, 'de', 'Gelber Röhrenaal', 'Sandige Hänge nahe Korallenriffen, etwa 7 bis 45 m tief', 'Westatlantik und Golf von Mexiko', 'Der Gelbe Röhrenaal ist ein Meeresfisch aus der Gruppe der Strahlenflosser und gehört zur Familie der Meeraale (Congridae), zu der die Meer- und Röhrenaale zählen.' FROM animals WHERE scientific_name = 'Heteroconger luteolus'
ON DUPLICATE KEY UPDATE common_name = VALUES(common_name), habitat = VALUES(habitat), range_region = VALUES(range_region), profile = VALUES(profile);
COMMIT;
