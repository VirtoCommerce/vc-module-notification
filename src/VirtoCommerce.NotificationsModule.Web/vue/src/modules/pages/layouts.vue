<template>
  <VcBlade
    :title="$t('NOTIFICATIONS.PAGES.LAYOUTS.TITLE')"
    :toolbar-items="bladeToolbar"
    :loading="loading"
    width="50%"
  >
    <VcDataTable
      v-model:search-value="searchValue"
      v-model:selection="selection"
      selection-mode="multiple"
      :items="items"
      :total-count="totalCount"
      :pagination="pagination"
      :searchable="true"
      :state-key="'NOTIFICATION_LAYOUTS'"
      :active-item-id="activeChildParam"
      :empty-state="{ icon: 'lucide-layout-template', title: $t('NOTIFICATIONS.PAGES.LAYOUTS.EMPTY') }"
      @row-click="(e: { data: NotificationLayout }) => open(e.data)"
      @pagination-click="pagination.goToPage"
    >
      <VcColumn
        id="name"
        :title="$t('NOTIFICATIONS.PAGES.LAYOUTS.COLUMNS.NAME')"
        body-class="ntf-list__name"
        always-visible
      />
      <VcColumn
        id="isDefault"
        :title="$t('NOTIFICATIONS.PAGES.LAYOUTS.COLUMNS.DEFAULT')"
      >
        <template #body="{ data }">
          <div
            v-if="data.isDefault"
            class="tw-inline-flex"
          >
            <VcStatus variant="success">{{ $t("NOTIFICATIONS.PAGES.LAYOUTS.DEFAULT") }}</VcStatus>
          </div>
        </template>
      </VcColumn>
      <VcColumn
        id="modifiedDate"
        :title="$t('NOTIFICATIONS.PAGES.TEMPLATES.COLUMNS.MODIFIED')"
        type="datetime"
      />
    </VcDataTable>
  </VcBlade>
</template>

<script setup lang="ts">
import { useAsync, useBlade, useDataTablePagination, useFunctions, useLoading, usePopup, useTableSearch, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcColumn, VcDataTable, VcStatus } from "@vc-shell/framework/ui";
import { computed, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import type { NotificationLayout } from "../../api_client/virtocommerce.notifications";
import { useNotificationClients } from "../api";

defineBlade({
  url: "/notification-layouts",
  name: "NotificationLayouts",
  permissions: ["notifications:read"],
});

const PAGE_SIZE = 20;

const { t } = useI18n({ useScope: "global" });
const { openBlade, exposeToChildren, closeChildren, activeChildParam } = useBlade();
const { showConfirmation } = usePopup();
const { debounce } = useFunctions();
const clients = useNotificationClients();

const items = ref<NotificationLayout[]>([]);
const selection = ref<NotificationLayout[]>([]);
const totalCount = ref(0);
const { searchValue } = useTableSearch({ stateKey: "NOTIFICATION_LAYOUTS" });
const pagination = useDataTablePagination({ stateKey: "NOTIFICATION_LAYOUTS", pageSize: PAGE_SIZE, totalCount });

const { loading: loadLoading, action: load } = useAsync(async () => {
  const result = await (await clients.layouts()).searchNotificationLayouts({
    keyword: searchValue.value || undefined,
    sort: "name",
    skip: pagination.skip,
    take: PAGE_SIZE,
  });
  items.value = result.results ?? [];
  totalCount.value = result.totalCount ?? 0;
  selection.value = [];
});

function open(layout?: NotificationLayout) {
  openBlade({ name: "NotificationLayoutDetails", param: layout?.id });
}

const { loading: removeLoading, action: removeSelected } = useAsync(async () => {
  await (await clients.layouts()).deleteNotificationLayout(selection.value.map((x) => x.id!));
  await load();
});

async function remove() {
  if (!(await showConfirmation(t("NOTIFICATIONS.ALERTS.DELETE_LAYOUTS", { count: selection.value.length })))) return;
  await closeChildren();
  await removeSelected();
}

const loading = useLoading(loadLoading, removeLoading);

const bladeToolbar = ref<IBladeToolbar[]>([
  { id: "refresh", title: t("NOTIFICATIONS.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
  {
    id: "add",
    title: t("NOTIFICATIONS.TOOLBAR.ADD"),
    icon: "lucide-plus",
    permissions: ["notifications:create"],
    clickHandler: () => open(),
  },
  {
    id: "delete",
    title: t("NOTIFICATIONS.TOOLBAR.DELETE"),
    icon: "lucide-trash-2",
    permissions: ["notifications:delete"],
    disabled: computed(() => !selection.value.length),
    clickHandler: remove,
  },
]);

watch(searchValue, () => pagination.setPage(1));
watch([searchValue, () => pagination.skip], debounce(load, 300));
onMounted(load);

exposeToChildren({ reload: load });
</script>
