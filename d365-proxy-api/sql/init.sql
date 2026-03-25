-- ============================================
-- D365 Tenant Config Table
-- ============================================
CREATE DATABASE IF NOT EXISTS `d365_proxy` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;

USE `d365_proxy`;

CREATE TABLE `d365_tenant_config` (
    `id`                BIGINT       NOT NULL AUTO_INCREMENT COMMENT '主键',
    `tenant_id`         VARCHAR(64)  NOT NULL COMMENT '租户ID / Azure Directory ID',
    `client_id`         VARCHAR(64)  NOT NULL COMMENT 'Azure App Client ID',
    `client_secret`     VARCHAR(256) NOT NULL COMMENT 'Azure App Client Secret',
    `d365_instance_url` VARCHAR(256) NOT NULL COMMENT 'D365 环境地址，如 https://org.crm.dynamics.com',
    `is_active`         TINYINT      NOT NULL DEFAULT 1 COMMENT '状态: 1=启用, 0=禁用',
    `create_time`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `update_time`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    PRIMARY KEY (`id`),
    UNIQUE INDEX `uk_tenant_id` (`tenant_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='D365 租户配置表';

-- 示例数据（按需修改）
-- INSERT INTO d365_tenant_config (tenant_id, client_id, client_secret, d365_instance_url)
-- VALUES ('xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx', 'your-client-id', 'your-client-secret', 'https://yourorg.crm.dynamics.com');
