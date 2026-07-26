namespace Beginor.SharpDb.Metadata;

internal sealed class PostgresMetadataProvider(
    IDbConnectionFactory connectionFactory,
    DatabaseOptions options
) : BaseMetadataProvider(connectionFactory, options) {

    protected override string GetTablesQuery() {
        // language=none
        return """
            with primary_keys as (
                select child_namespaces.nspname as table_schema,
                       child_classes.relname as table_name,
                       string_agg(child_attributes.attname, ', ' order by keys.ordinal_position) as primary_key_columns
                from pg_constraint constraints
                join pg_class child_classes
                  on child_classes.oid = constraints.conrelid
                join pg_namespace child_namespaces
                  on child_namespaces.oid = child_classes.relnamespace
                join unnest(constraints.conkey) with ordinality as keys(attribute_number, ordinal_position)
                  on true
                join pg_attribute child_attributes
                  on child_attributes.attrelid = child_classes.oid
                 and child_attributes.attnum = keys.attribute_number
                where constraints.contype = 'p'
                group by child_namespaces.nspname, child_classes.relname
            ),
            foreign_keys as (
                select child_namespaces.nspname as table_schema,
                       child_classes.relname as table_name,
                       string_agg(
                           child_attributes.attname || ' -> ' ||
                           referenced_namespaces.nspname || '.' ||
                           referenced_classes.relname || '(' ||
                           referenced_attributes.attname || ')',
                           '; '
                           order by constraints.conname, keys.ordinal_position
                       ) as foreign_keys,
                       string_agg(
                           distinct referenced_namespaces.nspname || '.' || referenced_classes.relname,
                           ', '
                       ) as related_objects
                from pg_constraint constraints
                join pg_class child_classes
                  on child_classes.oid = constraints.conrelid
                join pg_namespace child_namespaces
                  on child_namespaces.oid = child_classes.relnamespace
                join pg_class referenced_classes
                  on referenced_classes.oid = constraints.confrelid
                join pg_namespace referenced_namespaces
                  on referenced_namespaces.oid = referenced_classes.relnamespace
                join unnest(constraints.conkey, constraints.confkey) with ordinality
                    as keys(attribute_number, referenced_attribute_number, ordinal_position)
                  on true
                join pg_attribute child_attributes
                  on child_attributes.attrelid = child_classes.oid
                 and child_attributes.attnum = keys.attribute_number
                join pg_attribute referenced_attributes
                  on referenced_attributes.attrelid = referenced_classes.oid
                 and referenced_attributes.attnum = keys.referenced_attribute_number
                where constraints.contype = 'f'
                group by child_namespaces.nspname, child_classes.relname
            )
            select tables.table_schema,
                   tables.table_name,
                   tables.table_type,
                   obj_description(classes.oid, 'pg_class') as table_description,
                   primary_keys.primary_key_columns,
                   foreign_keys.foreign_keys,
                   foreign_keys.related_objects
            from information_schema.tables tables
            join pg_namespace namespaces
              on namespaces.nspname = tables.table_schema
            join pg_class classes
              on classes.relnamespace = namespaces.oid
             and classes.relname = tables.table_name
            left join primary_keys
              on primary_keys.table_schema = tables.table_schema
             and primary_keys.table_name = tables.table_name
            left join foreign_keys
              on foreign_keys.table_schema = tables.table_schema
             and foreign_keys.table_name = tables.table_name
            where tables.table_schema not in ('pg_catalog', 'information_schema')
              and tables.table_type in ('BASE TABLE', 'VIEW')
              and (@schema is null or @schema = '' or tables.table_schema = @schema)
            order by tables.table_schema, tables.table_name
            """;
    }

    protected override string GetColumnsQuery() {
        return """
            with primary_key_columns as (
                select child_namespaces.nspname as table_schema,
                       child_classes.relname as table_name,
                       child_attributes.attname as column_name
                from pg_constraint constraints
                join pg_class child_classes
                  on child_classes.oid = constraints.conrelid
                join pg_namespace child_namespaces
                  on child_namespaces.oid = child_classes.relnamespace
                join unnest(constraints.conkey) as keys(attribute_number)
                  on true
                join pg_attribute child_attributes
                  on child_attributes.attrelid = child_classes.oid
                 and child_attributes.attnum = keys.attribute_number
                where constraints.contype = 'p'
            ),
            foreign_key_columns as (
                select child_namespaces.nspname as table_schema,
                       child_classes.relname as table_name,
                       child_attributes.attname as column_name,
                       referenced_namespaces.nspname as referenced_table_schema,
                       referenced_classes.relname as referenced_table_name,
                       referenced_attributes.attname as referenced_column_name
                from pg_constraint constraints
                join pg_class child_classes
                  on child_classes.oid = constraints.conrelid
                join pg_namespace child_namespaces
                  on child_namespaces.oid = child_classes.relnamespace
                join pg_class referenced_classes
                  on referenced_classes.oid = constraints.confrelid
                join pg_namespace referenced_namespaces
                  on referenced_namespaces.oid = referenced_classes.relnamespace
                join unnest(constraints.conkey, constraints.confkey) with ordinality
                    as keys(attribute_number, referenced_attribute_number, ordinal_position)
                  on true
                join pg_attribute child_attributes
                  on child_attributes.attrelid = child_classes.oid
                 and child_attributes.attnum = keys.attribute_number
                join pg_attribute referenced_attributes
                  on referenced_attributes.attrelid = referenced_classes.oid
                 and referenced_attributes.attnum = keys.referenced_attribute_number
                where constraints.contype = 'f'
            )
            select columns.table_schema,
                   columns.table_name,
                   columns.column_name,
                   columns.ordinal_position,
                   columns.data_type,
                   columns.is_nullable,
                   columns.column_default,
                   col_description(classes.oid, attributes.attnum) as column_description,
                   case when primary_key_columns.column_name is null then 'NO' else 'YES' end as is_primary_key,
                   case when foreign_key_columns.column_name is null then 'NO' else 'YES' end as is_foreign_key,
                   foreign_key_columns.referenced_table_schema,
                   foreign_key_columns.referenced_table_name,
                   foreign_key_columns.referenced_column_name
            from information_schema.columns columns
            join pg_namespace namespaces
              on namespaces.nspname = columns.table_schema
            join pg_class classes
              on classes.relnamespace = namespaces.oid
             and classes.relname = columns.table_name
            join pg_attribute attributes
              on attributes.attrelid = classes.oid
             and attributes.attname = columns.column_name
            left join primary_key_columns
              on primary_key_columns.table_schema = columns.table_schema
             and primary_key_columns.table_name = columns.table_name
             and primary_key_columns.column_name = columns.column_name
            left join foreign_key_columns
              on foreign_key_columns.table_schema = columns.table_schema
             and foreign_key_columns.table_name = columns.table_name
             and foreign_key_columns.column_name = columns.column_name
            where columns.table_schema not in ('pg_catalog', 'information_schema')
              and columns.table_name = @tableName
              and (@schema is null or @schema = '' or columns.table_schema = @schema)
            order by columns.table_schema, columns.table_name, columns.ordinal_position
            """;
    }

}
