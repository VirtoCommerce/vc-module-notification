<template>
  <VcBlade
    :title="$t('NOTIFICATIONS.PAGES.LIST.TITLE')"
    :subtitle="options?.tenantName"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      data-key="type"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      :state-key="'NOTIFICATIONS_LIST'"
      :active-item-id="activeChildParam"
      :empty-state="{ icon: 'lucide-mail', title: $t('NOTIFICATIONS.PAGES.LIST.EMPTY') }"
      @row-click="(e: { data: NotificationEntity }) => open(e.data)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="type"
        :title="$t('NOTIFICATIONS.PAGES.LIST.COLUMNS.NAME')"
        body-class="ntf-list__name"
        always-visible
      >
        <template #body="{ data }">{{ displayName(data.type) }}</template>
      </VcColumn>
      <VcColumn
        id="kind"
        :title="$t('NOTIFICATIONS.PAGES.LIST.COLUMNS.KIND')"
      >
        <template #body="{ data }">
          <span class="ntf-kind">
            <VcIcon
              :icon="isEmail(data.kind) ? 'lucide-mail' : 'lucide-message-square'"
              size="s"
            />
            {{ $t(isEmail(data.kind) ? "NOTIFICATIONS.KIND.EMAIL" : "NOTIFICATIONS.KIND.SMS") }}
          </span>
        </template>
      </VcColumn>
      <VcColumn
        id="isActive"
        :title="$t('NOTIFICATIONS.PAGES.LIST.COLUMNS.STATUS')"
        always-visible
      >
        <template #body="{ data }">
          <div class="tw-inline-flex">
            <VcStatus :variant="data.isActive ? 'success' : 'danger'">
              {{ $t(data.isActive ? "NOTIFICATIONS.STATUS.ACTIVE" : "NOTIFICATIONS.STATUS.INACTIVE") }}
            </VcStatus>
          </div>
        </template>
      </VcColumn>
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, useDataTablePagination, useFunctions, useTableSearch, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcIcon, VcStatus } from "@vc-shell/framework/ui";
import { onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { displayName, isEmail, useNotificationClients, type NotificationEntity } from "../api";

defineBlade({
  url: "/notification-types",
  name: "NotificationsList",
  permissions: ["notifications:read"],
});

const PAGE_SIZE = 20;

const { t } = useI18n({ useScope: "global" });
// opened from a store: its notifications (tenant settings) instead of the global ones
const { options, openBlade, exposeToChildren, activeChildParam } = useBlade<{ tenantId?: string; tenantType?: string; tenantName?: string }>();
const { debounce } = useFunctions();
const clients = useNotificationClients();

const items = ref<NotificationEntity[]>([]);
const totalCount = ref(0);
const { searchValue } = useTableSearch({ stateKey: "NOTIFICATIONS_LIST" });
const pagination = useDataTablePagination({ stateKey: "NOTIFICATIONS_LIST", pageSize: PAGE_SIZE, totalCount });

const { loading, action: load } = useAsync(async () => {
  const result = await (await clients.notifications()).getNotifications({
    keyword: searchValue.value || undefined,
    tenantId: options.value?.tenantId,
    tenantType: options.value?.tenantType,
    skip: pagination.skip,
    take: PAGE_SIZE,
    responseGroup: "Default",
  });
  items.value = result.results ?? [];
  totalCount.value = result.totalCount ?? 0;
});

function open(item: NotificationEntity) {
  openBlade({
    name: "NotificationDetails",
    param: item.type,
    options: { tenantId: options.value?.tenantId, tenantType: options.value?.tenantType },
  });
}

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("NOTIFICATIONS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
]);

watch(searchValue, () => pagination.setPage(1));
watch([searchValue, () => pagination.skip], debounce(load, 300));
onMounted(load);

exposeToChildren({ reload: load });
</script>

<style lang="scss">
.ntf-kind {
  display: inline-flex;
  align-items: center;
  gap: 6px;
}
</style>
