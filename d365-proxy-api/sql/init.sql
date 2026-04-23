-- ============================================
-- D365 Tenant Config Table
-- ============================================
CREATE DATABASE IF NOT EXISTS `d365_proxy` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;

USE `d365_proxy`;

CREATE TABLE `d365_tenant_config` (
    `id`                BIGINT       NOT NULL AUTO_INCREMENT COMMENT '主键',
    `appId`         VARCHAR(64)  NOT NULL COMMENT '租户ID / Azure Directory ID',
    `tenantId`         VARCHAR(64)  NOT NULL COMMENT '租户ID / Azure Directory ID',
    `d365TenantId`         VARCHAR(64)  NOT NULL COMMENT 'd365的租户ID / Azure Directory ID',
    `d365ClientId`         VARCHAR(64)  NOT NULL COMMENT 'Azure App Client ID',
    `d365ClientSecret`     VARCHAR(256) NOT NULL COMMENT 'Azure App Client Secret',
    `d365InstanceUrl` VARCHAR(256) NOT NULL COMMENT 'D365 环境地址，如 https://org.crm.dynamics.com',
    `isActive`         TINYINT      NOT NULL DEFAULT 1 COMMENT '状态: 1=启用, 0=禁用',
    `createdDate`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `lastModifiedDate`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `deleted`       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE INDEX `uk_d365_tenant_id` (`d365TenantId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='D365 租户配置表';

-- 示例数据（按需修改）
-- INSERT INTO d365_tenant_config (tenant_id, client_id, client_secret, d365_instance_url)
-- VALUES ('xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx', 'your-client-id', 'your-client-secret', 'https://yourorg.crm.dynamics.com');
