-- 新增属性定义、模板与 EAV 值表
-- 请在测试库执行，执行前先备份数据库

CREATE TABLE IF NOT EXISTS `attribute_definitions` (
  `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  `key_name` VARCHAR(100) NOT NULL UNIQUE,        -- 内部键，例如: 'diameter'
  `display_name` VARCHAR(200) DEFAULT NULL,       -- 显示名，例如: '直径'
  `data_type` ENUM('string','number','date','json','bool') NOT NULL DEFAULT 'string',
  `unit` VARCHAR(50) DEFAULT NULL,
  `is_searchable` TINYINT(1) DEFAULT 0,
  `is_core_field` TINYINT(1) DEFAULT 0,           -- 是否应同步到 cad_file_attributes（高频）
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
  `category_id` INT NOT NULL,   -- 对应 cad_categories.id（或 cad_subcategories.id，按业务决定）
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
