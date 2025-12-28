-- 属性迁移脚本（测试环境运行，执行前请备份数据库）
-- 说明：
-- 1) 本脚本会创建 EAV/模板 相关表（若不存在），并把 cad_file_attributes 中选定的若干列迁移到 file_attribute_values（EAV）。
-- 2) 迁移为“增量写入”：不会删除原列数据。迁移验证无误后可按需清理 cad_file_attributes 中的数据或删除列。
-- 3) 在生产库执行前，务必在测试库跑通并核对结果。

-- ========== 1. 创建属性相关表 ==========
CREATE TABLE IF NOT EXISTS `attribute_definitions` (
  `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `key_name` VARCHAR(100) NOT NULL UNIQUE,
  `display_name` VARCHAR(200) DEFAULT NULL,
  `data_type` ENUM('string','number','date','json','bool') NOT NULL DEFAULT 'string',
  `unit` VARCHAR(50) DEFAULT NULL,
  `is_searchable` TINYINT(1) DEFAULT 0,
  `is_core_field` TINYINT(1) DEFAULT 0,
  `validation_regex` VARCHAR(500) DEFAULT NULL,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `attribute_templates` (
  `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `name` VARCHAR(200) NOT NULL,
  `description` TEXT DEFAULT NULL,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `attribute_template_items` (
  `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `template_id` INT NOT NULL,
  `attribute_id` INT NOT NULL,
  `required` TINYINT(1) DEFAULT 0,
  `display_order` INT DEFAULT 0,
  CONSTRAINT `fk_template_items_template` FOREIGN KEY (`template_id`) REFERENCES `attribute_templates`(`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_template_items_attribute` FOREIGN KEY (`attribute_id`) REFERENCES `attribute_definitions`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `category_attribute_templates` (
  `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `category_id` INT NOT NULL,
  `template_id` INT NOT NULL,
  CONSTRAINT `fk_cat_template_template` FOREIGN KEY (`template_id`) REFERENCES `attribute_templates`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `file_attribute_values` (
  `id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `file_id` INT NOT NULL,                 -- cad_file_storage.id
  `attribute_id` INT NOT NULL,            -- attribute_definitions.id
  `value_string` TEXT DEFAULT NULL,
  `value_number` DOUBLE DEFAULT NULL,
  `value_date` DATETIME DEFAULT NULL,
  `value_json` JSON DEFAULT NULL,
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  UNIQUE KEY `ux_file_attr` (`file_id`,`attribute_id`),
  INDEX `idx_attr_num` (`attribute_id`,`value_number`),
  INDEX `idx_attr_str` (`attribute_id`(50), `value_string`(100)),
  CONSTRAINT `fk_file_attr_attrdef` FOREIGN KEY (`attribute_id`) REFERENCES `attribute_definitions`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========== 2. 在 attribute_definitions 中注册将要迁移的属性 ==========
-- 这里示例性注册一组常用属性。若需更多属性，请按 key_name/数据类型追加。
INSERT INTO attribute_definitions (key_name, display_name, data_type, unit, is_searchable, is_core_field)
VALUES
  ('length','长度','number',NULL,0,0),
  ('width','宽度','number',NULL,0,0),
  ('height','高度','number',NULL,0,0),
  ('diameter','直径','string',NULL,1,0),
  ('outer_diameter','外径','string',NULL,0,0),
  ('inner_diameter','内径','string',NULL,0,0),
  ('material','材质','string',NULL,1,0),
  ('model','型号','string',NULL,1,0),
  ('weight','重量','string',NULL,0,0),
  ('pressure','压力','string',NULL,1,0),
  ('temperature','温度','string',NULL,0,0),
  ('flow','流量','string',NULL,0,0),
  ('manufacturer','制造商','string',NULL,1,0),
  ('standard_number','标准编号','string',NULL,0,0)
ON DUPLICATE KEY UPDATE display_name = VALUES(display_name);

-- ========== 3. 迁移数据：从 cad_file_attributes -> file_attribute_values ==========
-- 为安全起见，按列逐个迁移。下面示例按字符串/数值分别处理。

-- 数值列迁移： length, width, height
INSERT INTO file_attribute_values (file_id, attribute_id, value_number, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.length, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'length'
WHERE c.file_storage_id IS NOT NULL AND c.length IS NOT NULL
ON DUPLICATE KEY UPDATE value_number = VALUES(value_number), updated_at = CURRENT_TIMESTAMP;

INSERT INTO file_attribute_values (file_id, attribute_id, value_number, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.width, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'width'
WHERE c.file_storage_id IS NOT NULL AND c.width IS NOT NULL
ON DUPLICATE KEY UPDATE value_number = VALUES(value_number), updated_at = CURRENT_TIMESTAMP;

INSERT INTO file_attribute_values (file_id, attribute_id, value_number, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.height, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'height'
WHERE c.file_storage_id IS NOT NULL AND c.height IS NOT NULL
ON DUPLICATE KEY UPDATE value_number = VALUES(value_number), updated_at = CURRENT_TIMESTAMP;

-- 字符串列迁移示例： material, model, diameter, weight, pressure, flow, manufacturer, standard_number
INSERT INTO file_attribute_values (file_id, attribute_id, value_string, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.material, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'material'
WHERE c.file_storage_id IS NOT NULL AND c.material IS NOT NULL AND TRIM(c.material) <> ''
ON DUPLICATE KEY UPDATE value_string = VALUES(value_string), updated_at = CURRENT_TIMESTAMP;

INSERT INTO file_attribute_values (file_id, attribute_id, value_string, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.model, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'model'
WHERE c.file_storage_id IS NOT NULL AND c.model IS NOT NULL AND TRIM(c.model) <> ''
ON DUPLICATE KEY UPDATE value_string = VALUES(value_string), updated_at = CURRENT_TIMESTAMP;

INSERT INTO file_attribute_values (file_id, attribute_id, value_string, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.diameter, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'diameter'
WHERE c.file_storage_id IS NOT NULL AND c.diameter IS NOT NULL AND TRIM(c.diameter) <> ''
ON DUPLICATE KEY UPDATE value_string = VALUES(value_string), updated_at = CURRENT_TIMESTAMP;

INSERT INTO file_attribute_values (file_id, attribute_id, value_string, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.weight, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'weight'
WHERE c.file_storage_id IS NOT NULL AND c.weight IS NOT NULL AND TRIM(c.weight) <> ''
ON DUPLICATE KEY UPDATE value_string = VALUES(value_string), updated_at = CURRENT_TIMESTAMP;

INSERT INTO file_attribute_values (file_id, attribute_id, value_string, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.pressure, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'pressure'
WHERE c.file_storage_id IS NOT NULL AND c.pressure IS NOT NULL AND TRIM(c.pressure) <> ''
ON DUPLICATE KEY UPDATE value_string = VALUES(value_string), updated_at = CURRENT_TIMESTAMP;

INSERT INTO file_attribute_values (file_id, attribute_id, value_string, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.flow, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'flow'
WHERE c.file_storage_id IS NOT NULL AND c.flow IS NOT NULL AND TRIM(c.flow) <> ''
ON DUPLICATE KEY UPDATE value_string = VALUES(value_string), updated_at = CURRENT_TIMESTAMP;

-- 假如你有制造商字段在 cad_file_attributes（示例为 customize1 存厂商），可以迁移：
INSERT INTO file_attribute_values (file_id, attribute_id, value_string, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.customize1, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'manufacturer'
WHERE c.file_storage_id IS NOT NULL AND c.customize1 IS NOT NULL AND TRIM(c.customize1) <> ''
ON DUPLICATE KEY UPDATE value_string = VALUES(value_string), updated_at = CURRENT_TIMESTAMP;

INSERT INTO file_attribute_values (file_id, attribute_id, value_string, created_at, updated_at)
SELECT c.file_storage_id, ad.id, c.standard_number, NOW(), NOW()
FROM cad_file_attributes c
JOIN attribute_definitions ad ON ad.key_name = 'standard_number'
WHERE c.file_storage_id IS NOT NULL AND c.standard_number IS NOT NULL AND TRIM(c.standard_number) <> ''
ON DUPLICATE KEY UPDATE value_string = VALUES(value_string), updated_at = CURRENT_TIMESTAMP;

-- ========== 4. 验证迁移 ==========
-- 示例：统计迁移入库数量（按属性）
SELECT ad.key_name, COUNT(fav.id) AS migrated_count
FROM attribute_definitions ad
LEFT JOIN file_attribute_values fav ON fav.attribute_id = ad.id
WHERE ad.key_name IN ('length','width','height','material','model','diameter','weight','pressure','flow','manufacturer','standard_number')
GROUP BY ad.key_name;

-- ========== 5. （可选）清理或置空原表列 ==========
-- 在确认 file_attribute_values 中数据完全正确并备份后，可选择将 cad_file_attributes 中对应列置空或删除列。
-- 推荐先置空（保留列结构），运行一段时间观察系统稳定后再彻底删除列。
-- 将示例列清空（事务内执行）
START TRANSACTION;
UPDATE cad_file_attributes
SET length = NULL, width = NULL, height = NULL, diameter = NULL,
    material = NULL, model = NULL, weight = NULL, pressure = NULL, flow = NULL, customize1 = NULL, standard_number = NULL
WHERE file_storage_id IS NOT NULL;
COMMIT;

-- 若要删除列，请在备份并验证后谨慎执行（注：此操作不可逆）：
-- ALTER TABLE cad_file_attributes DROP COLUMN length, DROP COLUMN width, DROP COLUMN height, DROP COLUMN diameter, DROP COLUMN material, DROP COLUMN model, DROP COLUMN weight, DROP COLUMN pressure, DROP COLUMN flow, DROP COLUMN customize1, DROP COLUMN standard_number;

-- ========== 6. 示例：创建“泵”属性模板并关联到某主分类（示例 category_id=2000 表示工艺） ==========
INSERT INTO attribute_templates (name, description) VALUES ('泵模板', '泵常用属性模板：型号/材质/扬程/流量');
SET @tmpl_id = LAST_INSERT_ID();

-- 绑定模板项（假设 attribute_definitions 中已存在对应属性 key）
INSERT INTO attribute_template_items (template_id, attribute_id, required, display_order)
SELECT @tmpl_id, id, 1, 1 FROM attribute_definitions WHERE key_name = 'model' LIMIT 1;
INSERT INTO attribute_template_items (template_id, attribute_id, required, display_order)
SELECT @tmpl_id, id, 0, 2 FROM attribute_definitions WHERE key_name = 'material' LIMIT 1;
INSERT INTO attribute_template_items (template_id, attribute_id, required, display_order)
SELECT @tmpl_id, id, 0, 3 FROM attribute_definitions WHERE key_name = 'lift' LIMIT 1;
INSERT INTO attribute_template_items (template_id, attribute_id, required, display_order)
SELECT @tmpl_id, id, 0, 4 FROM attribute_definitions WHERE key_name = 'flow' LIMIT 1;

-- 将模板关联到主分类（示例：工艺假设 category_id=2000，根据你的主分类id替换）
INSERT INTO category_attribute_templates (category_id, template_id) VALUES (2000, @tmpl_id);

-- ========== 完成 ==========
-- 运行完脚本后：
-- 1) 在测试环境核对 file_attribute_values 中数据与 cad_file_attributes 源列数据一致性。
-- 2) 检查系统功能：新增/编辑图元是否能正确读写 EAV 值（你需要在应用层使用 SaveFileAttributeValueAsync/GetFileAttributeValuesAsync）。
-- 3) 迁移验证无误后再在生产环境运行（先备份）。
