<template>
  <div class="vehicle-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Vehicle Management</h1>
        <p class="section-subtitle">Track, assign, and maintain your fleet in one place</p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Vehicles</p>
        <h3>{{ vehicles.length }}</h3>
        <span class="stat-foot text-muted">Fleet size</span>
      </div>
      <div class="stat-card">
        <p>Active</p>
        <h3 class="text-success">{{ activeCount }}</h3>
        <span class="stat-foot text-muted">On the road</span>
      </div>
      <div class="stat-card">
        <p>In Maintenance</p>
        <h3 class="text-warning">{{ maintenanceCount }}</h3>
        <span class="stat-foot text-muted">Scheduled service</span>
      </div>
      <div class="stat-card">
        <p>Inactive</p>
        <h3 class="text-danger">{{ inactiveCount }}</h3>
        <span class="stat-foot text-muted">Unavailable</span>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search by vehicle ID, plate, or driver..."
          />
          <button
            v-if="searchQuery"
            class="clear-button"
            type="button"
            aria-label="Clear search"
            @click="searchQuery = ''"
          >
            <v-icon icon="mdi-close-circle" size="18" />
          </button>
        </div>

        <div class="toolbar-actions">
          <div class="toolbar-filter">
            <v-icon icon="mdi-filter-variant" />
            <select v-model="statusFilter">
              <option value="All">All Status</option>
              <option value="Active">Active</option>
              <option value="Maintenance">Maintenance</option>
              <option value="Inactive">Inactive</option>
            </select>
          </div>

          <button class="primary-button" type="button" @click="openAdd">
            <v-icon icon="mdi-truck-plus" size="18" />
            Add Vehicle
          </button>
        </div>
      </div>

      <div class="toolbar-count text-muted">
        Showing {{ filteredVehicles.length }} of {{ vehicles.length }} vehicles
      </div>
    </div>

    <div class="card-surface table-card">
      <div class="table-wrap">
        <v-data-table
          class="table-base vehicle-table"
          :headers="vehicleHeaders"
          :items="filteredVehicles"
          :items-per-page="10"
          :items-per-page-options="[10, 20, 30]"
          :mobile-breakpoint="0"
          :mobile="false"
          fixed-header
          height="520"
          density="comfortable"
        >
          <template #item.vehicle="{ item }">
            <div class="vehicle-cell">
              <button
                class="thumb-button tooltip"
                type="button"
                @click="openImage(item.image, item.type)"
              >
                <img :src="item.image" :alt="item.type" class="vehicle-image" />
                <span class="tooltip-text">View vehicle image</span>
              </button>
              <div>
                <strong>{{ item.id }}</strong>
                <div class="text-muted vehicle-sub">{{ item.model }}</div>
              </div>
            </div>
          </template>

          <template #item.plate="{ item }">
            <span class="text-muted">{{ item.plate }}</span>
          </template>

          <template #item.type="{ item }">
            <span>{{ item.type }}</span>
          </template>

          <template #item.status="{ item }">
            <span class="badge" :class="statusClass(item.status)">
              {{ item.status }}
            </span>
          </template>

          <template #item.driver="{ item }">
            <div class="driver-cell">
              <button
                class="thumb-button tooltip"
                type="button"
                @click="openImage(item.driverImage, item.driver)"
              >
                <img :src="item.driverImage" :alt="item.driver" class="driver-photo" />
                <span class="tooltip-text">View driver image</span>
              </button>
              <span>{{ item.driver }}</span>
            </div>
          </template>

          <template #item.acquiredDate="{ item }">
            <span class="text-muted">{{ formatDate(item.acquiredDate) }}</span>
          </template>

          <template #item.actions="{ item }">
            <div class="inline-actions">
              <button class="icon-button tooltip" type="button" @click="openEdit(item)">
                <v-icon icon="mdi-pencil-outline" size="18" />
                <span class="tooltip-text">Edit vehicle</span>
              </button>
              <button class="icon-button tooltip" type="button" @click="openDetails(item)">
                <v-icon icon="mdi-eye-outline" size="18" />
                <span class="tooltip-text">View details</span>
              </button>
              <button
                class="icon-button tooltip"
                :class="item.status === 'Active' ? 'warn' : 'good'"
                type="button"
                @click="toggleStatus(item.id)"
              >
                <v-icon icon="mdi-power" size="18" />
                <span class="tooltip-text">
                  {{ item.status === 'Active' ? 'Set inactive' : 'Set active' }}
                </span>
              </button>
              <button class="icon-button danger tooltip" type="button" @click="deleteVehicle(item.id)">
                <v-icon icon="mdi-trash-can-outline" size="18" />
                <span class="tooltip-text">Delete vehicle</span>
              </button>
            </div>
          </template>

          <template #no-data>
            <div class="empty-state">No vehicles found matching your criteria</div>
          </template>
        </v-data-table>
      </div>
    </div>

    <div class="card-surface section-card table-card">
      <div class="section-header">
        <div>
          <div class="section-title">Accident & Incident Records</div>
          <div class="text-muted section-subtitle">Track claims, costs, and follow-ups</div>
        </div>
        <div class="section-actions">
          <button class="primary-button" type="button" @click="openIncident">
            <v-icon icon="mdi-alert-circle-outline" size="18" />
            Report Incident
          </button>
        </div>
      </div>
      <div class="table-wrap">
        <table class="table-base">
          <thead>
            <tr>
              <th>Date</th>
              <th>Vehicle</th>
              <th>Type</th>
              <th>Severity</th>
              <th>Status</th>
              <th class="align-right">Cost</th>
              <th class="align-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="incident in pagedIncidents" :key="incident.id">
              <td class="text-muted" data-label="Date">{{ formatDate(incident.date) }}</td>
              <td data-label="Vehicle">
                <strong>{{ incident.vehicleId }}</strong>
                <div class="text-muted vehicle-sub">{{ incident.driver }}</div>
              </td>
              <td data-label="Type">{{ incident.type }}</td>
              <td data-label="Severity">
                <span class="badge" :class="severityClass(incident.severity)">
                  {{ incident.severity }}
                </span>
              </td>
              <td data-label="Status">
                <span class="badge" :class="incident.status === 'Open' ? 'warning' : 'success'">
                  {{ incident.status }}
                </span>
              </td>
              <td class="align-right" data-label="Cost">{{ incident.cost || '—' }}</td>
              <td class="align-right" data-label="Actions">
                <div class="inline-actions">
                  <button class="icon-button tooltip" type="button" @click="openIncidentDetails(incident)">
                    <v-icon icon="mdi-eye-outline" size="18" />
                    <span class="tooltip-text">View details</span>
                  </button>
                  <button class="icon-button tooltip" type="button" @click="openIncidentEdit(incident)">
                    <v-icon icon="mdi-pencil-outline" size="18" />
                    <span class="tooltip-text">Edit incident</span>
                  </button>
                  <button class="icon-button danger tooltip" type="button" @click="deleteIncident(incident.id)">
                    <v-icon icon="mdi-trash-can-outline" size="18" />
                    <span class="tooltip-text">Delete incident</span>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-if="incidentTotalPages > 1" class="table-footer">
        <span class="pager-info text-muted">Page {{ incidentSafePage }} of {{ incidentTotalPages }}</span>
        <div class="pager-actions">
          <button
            class="pager-button"
            type="button"
            :disabled="incidentSafePage === 1"
            @click="incidentPage = incidentSafePage - 1"
          >
            Prev
          </button>
          <button
            class="pager-button"
            type="button"
            :disabled="incidentSafePage === incidentTotalPages"
            @click="incidentPage = incidentSafePage + 1"
          >
            Next
          </button>
        </div>
      </div>
      <div v-if="incidents.length === 0" class="empty-state">
        No incidents recorded yet
      </div>
    </div>

    <v-dialog v-model="detailsOpen" max-width="960">
      <div v-if="selectedVehicle" class="card-surface details-card">
        <div class="details-header">
          <div>
            <div class="details-title">
              {{ selectedVehicle.id }} • {{ selectedVehicle.plate }}
            </div>
            <div class="details-subtitle text-muted">
              {{ selectedVehicle.model }} • {{ selectedVehicle.type }}
            </div>
          </div>
          <button class="icon-button" type="button" @click="detailsOpen = false">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div class="details-grid">
          <div class="details-section">
            <h4>Overview</h4>
            <div class="details-row"><span>Region</span><strong>{{ selectedVehicle.region }}</strong></div>
            <div class="details-row"><span>Driver</span><strong>{{ selectedVehicle.driver }}</strong></div>
            <div class="details-row"><span>Depot</span><strong>{{ selectedVehicle.depot }}</strong></div>
            <div class="details-row"><span>Status</span><strong>{{ selectedVehicle.status }}</strong></div>
            <div class="details-row"><span>Capacity</span><strong>{{ selectedVehicle.capacity }}</strong></div>
            <div class="details-row"><span>Fuel Type</span><strong>{{ selectedVehicle.fuelType }}</strong></div>
          </div>

          <div class="details-section">
            <h4>Identity</h4>
            <div class="details-row"><span>VIN / Chassis</span><strong>{{ selectedVehicle.vin }}</strong></div>
            <div class="details-row"><span>Engine No.</span><strong>{{ selectedVehicle.engineNo }}</strong></div>
            <div class="details-row"><span>Odometer</span><strong>{{ selectedVehicle.odometer }}</strong></div>
            <div class="details-row"><span>Acquired</span><strong>{{ formatDate(selectedVehicle.acquiredDate) }}</strong></div>
          </div>

          <div class="details-section">
            <h4>Specs & Ownership</h4>
            <div class="details-row"><span>Make</span><strong>{{ selectedVehicle.make || '—' }}</strong></div>
            <div class="details-row"><span>Year</span><strong>{{ selectedVehicle.year || '—' }}</strong></div>
            <div class="details-row"><span>Color</span><strong>{{ selectedVehicle.color || '—' }}</strong></div>
            <div class="details-row"><span>Ownership</span><strong>{{ selectedVehicle.ownership || '—' }}</strong></div>
            <div class="details-row"><span>Purchase Cost</span><strong>{{ selectedVehicle.purchaseCost || '—' }}</strong></div>
            <div class="details-row"><span>Fuel Capacity</span><strong>{{ selectedVehicle.fuelCapacity || '—' }}</strong></div>
          </div>

          <div class="details-section">
            <h4>Compliance</h4>
            <div class="details-row"><span>Registration No.</span><strong>{{ selectedVehicle.registrationNo || '—' }}</strong></div>
            <div class="details-row"><span>Registration Expiry</span><strong>{{ formatDate(selectedVehicle.registrationExpiry) }}</strong></div>
            <div class="details-row"><span>Road Tax Expiry</span><strong>{{ formatDate(selectedVehicle.roadTaxExpiry) }}</strong></div>
            <div class="details-row"><span>Insurance Expiry</span><strong>{{ formatDate(selectedVehicle.insuranceExpiry) }}</strong></div>
            <div class="details-row"><span>Insurance Provider</span><strong>{{ selectedVehicle.insuranceProvider || '—' }}</strong></div>
            <div class="details-row"><span>Policy No.</span><strong>{{ selectedVehicle.insurancePolicy || '—' }}</strong></div>
            <div class="details-row"><span>Inspection Due</span><strong>{{ formatDate(selectedVehicle.inspectionDue) }}</strong></div>
          </div>

          <div class="details-section">
            <h4>Maintenance</h4>
            <div class="details-row"><span>Last Service</span><strong>{{ formatDate(selectedVehicle.lastService) }}</strong></div>
            <div class="details-row"><span>Next Service</span><strong>{{ formatDate(selectedVehicle.nextService) }}</strong></div>
            <div class="details-row"><span>Service Note</span><strong>{{ selectedVehicle.serviceNote }}</strong></div>
          </div>

        </div>
      </div>
    </v-dialog>

    <v-dialog v-model="formOpen" max-width="960">
      <div class="card-surface form-card">
        <div class="form-header">
          <div class="form-title">{{ formMode === 'edit' ? 'Edit Vehicle' : 'Add Vehicle' }}</div>
          <button class="icon-button" type="button" @click="closeForm">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div v-if="formError" class="form-error">{{ formError }}</div>

        <div class="form-steps">
          <div
            v-for="step in formSteps"
            :key="step.id"
            class="form-step"
            :class="{ active: formStep === step.id, done: formStep > step.id }"
          >
            <div class="step-index">{{ step.id }}</div>
            <div>
              <div class="step-title">{{ step.title }}</div>
              <div class="text-muted step-subtitle">{{ step.subtitle }}</div>
            </div>
          </div>
        </div>

        <div v-if="formStep === 1" class="form-grid">
          <div class="form-field">
            <label>Plate Number <span class="required">*</span></label>
            <input v-model="formData.plate" type="text" placeholder="e.g., YGN-7742" />
          </div>
          <div class="form-field">
            <label>Region <span class="required">*</span></label>
            <input v-model="formData.region" type="text" placeholder="e.g., Yangon" />
          </div>
          <div class="form-field">
            <label>Vehicle Type <span class="required">*</span></label>
            <input v-model="formData.type" type="text" placeholder="e.g., Box Truck" />
          </div>
          <div class="form-field">
            <label>Model <span class="required">*</span></label>
            <input v-model="formData.model" type="text" placeholder="e.g., Isuzu FVR" />
          </div>
          <div class="form-field">
            <label>Make</label>
            <input v-model="formData.make" type="text" placeholder="e.g., Isuzu" />
          </div>
          <div class="form-field">
            <label>Year</label>
            <input v-model="formData.year" type="number" min="1980" max="2100" placeholder="e.g., 2022" />
          </div>
          <div class="form-field">
            <label>Color</label>
            <input v-model="formData.color" type="text" placeholder="e.g., White" />
          </div>
          <div class="form-field">
            <label>Status <span class="required">*</span></label>
            <select v-model="formData.status">
              <option value="Active">Active</option>
              <option value="Maintenance">Maintenance</option>
              <option value="Inactive">Inactive</option>
            </select>
          </div>
          <div class="form-field">
            <label>Ownership</label>
            <select v-model="formData.ownership">
              <option value="Owned">Owned</option>
              <option value="Leased">Leased</option>
            </select>
          </div>
          <div class="form-field">
            <label>Driver <span class="required">*</span></label>
            <input v-model="formData.driver" type="text" placeholder="Driver name" />
          </div>
          <div class="form-field">
            <label>
              Depot
              <span class="hint tooltip icon-tooltip" tabindex="0" aria-label="Home base or yard">
                <v-icon icon="mdi-information-outline" size="14" />
                <span class="tooltip-text">Home base / yard</span>
              </span>
            </label>
            <input v-model="formData.depot" type="text" placeholder="Depot / yard" />
          </div>
          <div class="form-field">
            <label>Capacity</label>
            <input v-model="formData.capacity" type="text" placeholder="e.g., 6 tons" />
          </div>
          <div class="form-field">
            <label>Fuel Capacity</label>
            <input v-model="formData.fuelCapacity" type="text" placeholder="e.g., 120 L" />
          </div>
          <div class="form-field">
            <label>Fuel Type <span class="required">*</span></label>
            <input v-model="formData.fuelType" type="text" placeholder="e.g., Diesel" />
          </div>
          <div class="form-field">
            <label>VIN / Chassis</label>
            <input v-model="formData.vin" type="text" placeholder="VIN / chassis" />
          </div>
          <div class="form-field">
            <label>Engine No.</label>
            <input v-model="formData.engineNo" type="text" placeholder="Engine number" />
          </div>
          <div class="form-field">
            <label>
              Odometer
              <span
                class="hint tooltip icon-tooltip"
                tabindex="0"
                aria-label="Total distance traveled in kilometers"
              >
                <v-icon icon="mdi-information-outline" size="14" />
                <span class="tooltip-text">Total distance (km)</span>
              </span>
            </label>
            <input v-model="formData.odometer" type="text" placeholder="e.g., 120,000 km" />
          </div>
          <div class="form-field">
            <label>Acquired Date</label>
            <input v-model="formData.acquiredDate" type="date" />
          </div>
        </div>

        <div v-if="formStep === 2" class="form-grid">
          <div class="form-field">
            <label>Purchase Cost</label>
            <input v-model="formData.purchaseCost" type="text" placeholder="e.g., $48,000" />
          </div>
          <div class="form-field">
            <label>Registration Number</label>
            <input v-model="formData.registrationNo" type="text" placeholder="Registration number" />
          </div>
          <div class="form-field">
            <label>
              Registration Expiry
              <span
                class="hint tooltip icon-tooltip"
                tabindex="0"
                aria-label="Date the registration must be renewed"
              >
                <v-icon icon="mdi-information-outline" size="14" />
                <span class="tooltip-text">Renewal date</span>
              </span>
            </label>
            <input v-model="formData.registrationExpiry" type="date" />
          </div>
          <div class="form-field">
            <label>Road Tax Expiry</label>
            <input v-model="formData.roadTaxExpiry" type="date" />
          </div>
          <div class="form-field">
            <label>Insurance Expiry</label>
            <input v-model="formData.insuranceExpiry" type="date" />
          </div>
          <div class="form-field">
            <label>Insurance Provider</label>
            <input v-model="formData.insuranceProvider" type="text" placeholder="Provider name" />
          </div>
          <div class="form-field">
            <label>Policy Number</label>
            <input v-model="formData.insurancePolicy" type="text" placeholder="Policy / certificate no." />
          </div>
          <div class="form-field">
            <label>
              Inspection Due
              <span
                class="hint tooltip icon-tooltip"
                tabindex="0"
                aria-label="Next required safety inspection"
              >
                <v-icon icon="mdi-information-outline" size="14" />
                <span class="tooltip-text">Next safety check</span>
              </span>
            </label>
            <input v-model="formData.inspectionDue" type="date" />
          </div>
          <div class="form-field">
            <label>Last Service</label>
            <input v-model="formData.lastService" type="date" />
          </div>
          <div class="form-field">
            <label>Next Service</label>
            <input v-model="formData.nextService" type="date" />
          </div>
          <div class="form-field">
            <label>Service Note</label>
            <input v-model="formData.serviceNote" type="text" placeholder="Service note" />
          </div>
        </div>

        <div v-if="formStep === 3" class="form-grid">
          <div class="form-field">
            <label>Vehicle Image URL</label>
            <input v-model="formData.image" type="url" placeholder="https://..." />
          </div>
          <div class="form-field">
            <label>Driver Image URL</label>
            <input v-model="formData.driverImage" type="url" placeholder="https://..." />
          </div>
        </div>

        <div class="form-actions">
          <button class="ghost-button" type="button" @click="closeForm">Cancel</button>
          <button class="ghost-button" type="button" @click="prevFormStep" :disabled="formStep === 1">
            Back
          </button>
          <button
            v-if="formStep < formSteps.length"
            class="primary-button"
            type="button"
            @click="nextFormStep"
            :disabled="!canGoNext"
          >
            Next
          </button>
          <button
            v-else
            class="primary-button"
            type="button"
            @click="saveForm"
            :disabled="!canSubmit"
          >
            {{ formMode === 'edit' ? 'Save Changes' : 'Add Vehicle' }}
          </button>
        </div>
      </div>
    </v-dialog>

    <v-dialog v-model="incidentOpen" max-width="720">
      <div class="card-surface form-card">
        <div class="form-header">
        <div class="form-title">{{ incidentMode === 'edit' ? 'Edit Incident' : 'Report Incident' }}</div>
          <button class="icon-button" type="button" @click="incidentOpen = false">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div v-if="incidentError" class="form-error">{{ incidentError }}</div>

        <div class="form-grid">
          <div class="form-field">
            <label>Vehicle</label>
            <select v-model="incidentForm.vehicleId">
              <option disabled value="">Select vehicle</option>
              <option v-for="vehicle in vehicles" :key="vehicle.id" :value="vehicle.id">
                {{ vehicle.id }} • {{ vehicle.model }}
              </option>
            </select>
          </div>
          <div class="form-field">
            <label>Driver</label>
            <input v-model="incidentForm.driver" type="text" placeholder="Driver name" />
          </div>
          <div class="form-field">
            <label>Date</label>
            <input v-model="incidentForm.date" type="date" />
          </div>
          <div class="form-field">
            <label>Type</label>
            <input v-model="incidentForm.type" type="text" placeholder="e.g., Collision" />
          </div>
          <div class="form-field">
            <label>Severity</label>
            <select v-model="incidentForm.severity">
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </div>
          <div class="form-field">
            <label>Status</label>
            <select v-model="incidentForm.status">
              <option value="Open">Open</option>
              <option value="Closed">Closed</option>
            </select>
          </div>
          <div class="form-field">
            <label>Cost</label>
            <input v-model="incidentForm.cost" type="text" placeholder="e.g., $1,250" />
          </div>
          <div class="form-field">
            <label>Notes</label>
            <input v-model="incidentForm.notes" type="text" placeholder="Summary of incident" />
          </div>
        </div>

        <div class="form-actions">
          <button class="ghost-button" type="button" @click="incidentOpen = false">Cancel</button>
          <button class="primary-button" type="button" @click="saveIncident">
            {{ incidentMode === 'edit' ? 'Save Changes' : 'Save Incident' }}
          </button>
        </div>
      </div>
    </v-dialog>

    <v-dialog v-model="incidentDetailsOpen" max-width="720">
      <div v-if="selectedIncident" class="card-surface details-card">
        <div class="details-header">
          <div>
            <div class="details-title">Incident {{ selectedIncident.id }}</div>
            <div class="details-subtitle text-muted">
              {{ selectedIncident.vehicleId }} • {{ selectedIncident.type }}
            </div>
          </div>
          <button class="icon-button" type="button" @click="incidentDetailsOpen = false">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div class="details-grid">
          <div class="details-section">
            <h4>Overview</h4>
            <div class="details-row"><span>Date</span><strong>{{ formatDate(selectedIncident.date) }}</strong></div>
            <div class="details-row"><span>Driver</span><strong>{{ selectedIncident.driver || '—' }}</strong></div>
            <div class="details-row"><span>Status</span><strong>{{ selectedIncident.status }}</strong></div>
            <div class="details-row"><span>Severity</span><strong>{{ selectedIncident.severity }}</strong></div>
          </div>
          <div class="details-section">
            <h4>Claims</h4>
            <div class="details-row"><span>Cost</span><strong>{{ selectedIncident.cost || '—' }}</strong></div>
            <div class="details-row"><span>Notes</span><strong>{{ selectedIncident.notes || '—' }}</strong></div>
          </div>
        </div>
      </div>
    </v-dialog>

    <v-dialog v-model="imageOpen" max-width="720">
      <div class="card-surface image-modal">
        <div class="image-header">
          <div class="image-title">{{ imageTitle }}</div>
          <button class="icon-button" type="button" @click="imageOpen = false">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>
        <img v-if="imageSrc" :src="imageSrc" :alt="imageTitle" class="full-image" />
      </div>
    </v-dialog>

    <ConfirmDialog
      :open="confirmOpen"
      :title="confirmTitle"
      :message="confirmMessage"
      :confirm-text="confirmButton"
      :tone="confirmTone"
      @confirm="runConfirm"
      @cancel="confirmOpen = false"
    />
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'

const vehicles = ref([
  {
    id: 'VH-2048',
    plate: 'BRC-4521',
    region: 'Bago',
    type: 'Box Truck',
    model: 'Volvo FL 280',
    status: 'Active',
    driver: 'Sarah Johnson',
    driverImage: 'https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=crop&w=800&q=80',
    depot: 'Yangon East Yard',
    capacity: '6 tons',
    fuelType: 'Diesel',
    vin: 'MMTFL280X7A2048',
    engineNo: 'ENG-2048-XY',
    odometer: '182,450 km',
    lastService: '2025-11-10',
    nextService: '2026-04-10',
    serviceNote: 'Brake pads replaced',
    registrationExpiry: '2026-09-30',
    roadTaxExpiry: '2026-06-30',
    insuranceExpiry: '2026-08-15',
    inspectionDue: '2026-05-20',
    acquiredDate: '2017-06-14',
    image: 'https://images.unsplash.com/photo-1489515217757-5fd1be406fef?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'MM-3047',
    plate: 'YGN-1187',
    region: 'Nay Pyi Taw',
    type: 'Alphard',
    model: 'Alphard FL 280',
    status: 'Active',
    driver: 'Sarah Johnson',
    driverImage: 'https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=crop&w=800&q=80',
    depot: 'Yangon East Yard',
    capacity: '6 tons',
    fuelType: 'Diesel',
    vin: 'MMTFL280X7A2048',
    engineNo: 'ENG-2048-XY',
    odometer: '182,450 km',
    lastService: '2025-11-10',
    nextService: '2026-04-10',
    serviceNote: 'Brake pads replaced',
    registrationExpiry: '2026-09-30',
    roadTaxExpiry: '2026-06-30',
    insuranceExpiry: '2026-08-15',
    inspectionDue: '2026-05-20',
    acquiredDate: '2017-06-14',
    image: 'https://images.unsplash.com/photo-1489515217757-5fd1be406fef?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'VH-3054',
    plate: 'MDY-1109',
    region: 'Mandalay',
    type: 'Cargo Van',
    model: 'Ford Transit',
    status: 'Maintenance',
    driver: 'Michael Chen',
    driverImage: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=800&q=80',
    depot: 'Mandalay Hub',
    capacity: '2 tons',
    fuelType: 'Diesel',
    vin: 'MMTRNS3054F1109',
    engineNo: 'ENG-3054-AK',
    odometer: '96,880 km',
    lastService: '2026-01-06',
    nextService: '2026-03-22',
    serviceNote: 'Transmission inspection',
    registrationExpiry: '2026-10-12',
    roadTaxExpiry: '2026-07-31',
    insuranceExpiry: '2026-09-02',
    inspectionDue: '2026-04-18',
    acquiredDate: '2019-03-22',
    image: 'https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'VH-1987',
    plate: 'YGN-7742',
    region: 'Yangon',
    type: 'Reefer Truck',
    model: 'Isuzu FVR',
    status: 'Active',
    driver: 'Emily Davis',
    driverImage: 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=800&q=80',
    depot: 'Thanlyin Cold Chain',
    capacity: '8 tons',
    fuelType: 'Diesel',
    vin: 'MMFVR1987YGN7742',
    engineNo: 'ENG-1987-FR',
    odometer: '143,220 km',
    lastService: '2025-12-02',
    nextService: '2026-04-25',
    serviceNote: 'Reefer unit serviced',
    registrationExpiry: '2026-08-05',
    roadTaxExpiry: '2026-06-10',
    insuranceExpiry: '2026-07-19',
    inspectionDue: '2026-05-02',
    acquiredDate: '2018-11-08',
    image: 'https://images.unsplash.com/photo-1517940310602-26535839fe84?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'VH-4129',
    plate: 'NPT-2306',
    region: 'Naypyitaw',
    type: 'Flatbed',
    model: 'Hino 500',
    status: 'Inactive',
    driver: 'Robert Wilson',
    driverImage: 'https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=800&q=80',
    depot: 'Naypyitaw Central',
    capacity: '10 tons',
    fuelType: 'Diesel',
    vin: 'MMHINO4129NPT2306',
    engineNo: 'ENG-4129-HN',
    odometer: '210,540 km',
    lastService: '2025-09-18',
    nextService: '2026-02-28',
    serviceNote: 'Awaiting tire replacement',
    registrationExpiry: '2026-04-20',
    roadTaxExpiry: '2026-03-31',
    insuranceExpiry: '2026-05-14',
    inspectionDue: '2026-03-20',
    acquiredDate: '2016-02-17',
    image: 'https://images.unsplash.com/photo-1513735717081-8ad5c3c244eb?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'VH-2661',
    plate: 'BGO-5584',
    region: 'Bago',
    type: 'Delivery Van',
    model: 'Mercedes Sprinter',
    status: 'Active',
    driver: 'John Martinez',
    driverImage: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=800&q=80',
    depot: 'Bago Cross-Dock',
    capacity: '1.5 tons',
    fuelType: 'Diesel',
    vin: 'MMSPR2661BGO5584',
    engineNo: 'ENG-2661-MS',
    odometer: '78,930 km',
    lastService: '2026-02-01',
    nextService: '2026-06-01',
    serviceNote: 'Oil + filter changed',
    registrationExpiry: '2027-01-11',
    roadTaxExpiry: '2026-11-30',
    insuranceExpiry: '2026-12-19',
    inspectionDue: '2026-09-10',
    acquiredDate: '2020-09-30',
    image: 'https://images.unsplash.com/photo-1511919884226-fd3cad34687c?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'VH-3775',
    plate: 'MND-9021',
    region: 'Mandalay',
    type: 'Tanker',
    model: 'Kenworth T800',
    status: 'Maintenance',
    driver: 'Amanda Taylor',
    driverImage: 'https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=crop&w=800&q=80',
    depot: 'Monywa Depot',
    capacity: '12 tons',
    fuelType: 'Diesel',
    vin: 'MMTNK3775MND9021',
    engineNo: 'ENG-3775-TK',
    odometer: '256,010 km',
    lastService: '2026-01-15',
    nextService: '2026-03-30',
    serviceNote: 'Pump calibration',
    registrationExpiry: '2026-07-02',
    roadTaxExpiry: '2026-06-15',
    insuranceExpiry: '2026-08-07',
    inspectionDue: '2026-04-12',
    acquiredDate: '2017-12-05',
    image: 'https://images.unsplash.com/photo-1517148815978-75f6acaaf32c?auto=format&fit=crop&w=1200&q=80'
  }
])

const incidents = ref([
  {
    id: 'INC-1024',
    vehicleId: 'VH-2048',
    driver: 'Sarah Johnson',
    date: '2026-01-24',
    type: 'Minor collision',
    severity: 'Low',
    status: 'Closed',
    cost: '$580',
    notes: 'Rear bumper repair'
  },
  {
    id: 'INC-1091',
    vehicleId: 'VH-3054',
    driver: 'Michael Chen',
    date: '2026-02-18',
    type: 'Windshield crack',
    severity: 'Medium',
    status: 'Open',
    cost: '$1,220',
    notes: 'Awaiting glass replacement'
  }
])

const incidentPage = ref(1)
const incidentPageSize = 5


const searchQuery = ref('')
const debouncedVehicleQuery = ref('')
const statusFilter = ref('All')
const confirmOpen = ref(false)
const confirmTitle = ref('Are you sure?')
const confirmMessage = ref('')
const confirmButton = ref('Confirm')
const confirmTone = ref('danger')
const pendingAction = ref(() => {})
const detailsOpen = ref(false)
const selectedVehicle = ref(null)
const imageOpen = ref(false)
const imageSrc = ref('')
const imageTitle = ref('')
const formOpen = ref(false)
const formMode = ref('add')
const formError = ref('')
const formData = ref({})
const formStep = ref(1)
const formSteps = [
  { id: 1, title: 'Core Info', subtitle: 'Identity, ownership, assignment' },
  { id: 2, title: 'Compliance', subtitle: 'Registration, insurance, service' },
  { id: 3, title: 'Images', subtitle: 'Vehicle and driver photos' }
]
const incidentOpen = ref(false)
const incidentMode = ref('add')
const incidentError = ref('')
const incidentForm = ref({})
const incidentDetailsOpen = ref(false)
const selectedIncident = ref(null)

const filteredVehicles = computed(() => {
  const query = debouncedVehicleQuery.value.toLowerCase()
  return vehicles.value.filter((vehicle) => {
    const matchesSearch =
      vehicle.id.toLowerCase().includes(query) ||
      vehicle.plate.toLowerCase().includes(query) ||
      vehicle.driver.toLowerCase().includes(query)
    const matchesStatus = statusFilter.value === 'All' || vehicle.status === statusFilter.value
    return matchesSearch && matchesStatus
  })
})

let vehicleSearchTimer = null
watch(
  () => searchQuery.value,
  (value) => {
    if (vehicleSearchTimer) clearTimeout(vehicleSearchTimer)
    vehicleSearchTimer = setTimeout(() => {
      debouncedVehicleQuery.value = value
    }, 350)
  },
  { immediate: true }
)

onBeforeUnmount(() => {
  if (vehicleSearchTimer) clearTimeout(vehicleSearchTimer)
})

const vehicleHeaders = [
  { title: 'Vehicle', key: 'vehicle' },
  { title: 'Plate Number', key: 'plate' },
  { title: 'Type', key: 'type' },
  { title: 'Status', key: 'status' },
  { title: 'Driver Assigned', key: 'driver' },
  { title: 'Acquired Date', key: 'acquiredDate' },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false }
]

const incidentTotalPages = computed(() =>
  Math.max(1, Math.ceil(incidents.value.length / incidentPageSize))
)
const incidentSafePage = computed(() => Math.min(incidentPage.value, incidentTotalPages.value))
const pagedIncidents = computed(() => {
  const start = (incidentSafePage.value - 1) * incidentPageSize
  return incidents.value.slice(start, start + incidentPageSize)
})


const activeCount = computed(() => vehicles.value.filter((v) => v.status === 'Active').length)
const maintenanceCount = computed(() => vehicles.value.filter((v) => v.status === 'Maintenance').length)
const inactiveCount = computed(() => vehicles.value.filter((v) => v.status === 'Inactive').length)

const stepOneValid = computed(
  () =>
    !!formData.value.plate &&
    !!formData.value.region &&
    !!formData.value.type &&
    !!formData.value.model &&
    !!formData.value.status &&
    !!formData.value.driver &&
    !!formData.value.fuelType
)

const canGoNext = computed(() => (formStep.value === 1 ? stepOneValid.value : true))

const canSubmit = computed(() => stepOneValid.value)

const statusClass = (status) => {
  if (status === 'Active') return 'success'
  if (status === 'Maintenance') return 'warning'
  return 'neutral'
}

const severityClass = (severity) => {
  if (severity === 'High') return 'danger'
  if (severity === 'Medium') return 'warning'
  return 'success'
}

const formatDate = (value) =>
  value
    ? new Date(value).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    })
    : '—'

const openDetails = (vehicle) => {
  selectedVehicle.value = vehicle
  detailsOpen.value = true
}

const openImage = (src, title) => {
  imageSrc.value = src
  imageTitle.value = title
  imageOpen.value = true
}

const buildEmptyForm = () => ({
  id: '',
  plate: '',
  region: '',
  type: '',
  model: '',
  make: '',
  year: '',
  color: '',
  status: 'Active',
  ownership: 'Owned',
  driver: '',
  driverImage: '',
  depot: '',
  capacity: '',
  fuelCapacity: '',
  fuelType: '',
  vin: '',
  engineNo: '',
  odometer: '',
  lastService: '',
  nextService: '',
  serviceNote: '',
  purchaseCost: '',
  registrationNo: '',
  registrationExpiry: '',
  roadTaxExpiry: '',
  insuranceExpiry: '',
  insuranceProvider: '',
  insurancePolicy: '',
  inspectionDue: '',
  acquiredDate: '',
  image: ''
})

const buildEmptyIncident = () => ({
  id: '',
  vehicleId: '',
  driver: '',
  date: '',
  type: '',
  severity: 'Low',
  status: 'Open',
  cost: '',
  notes: ''
})


const openAdd = () => {
  formMode.value = 'add'
  formData.value = buildEmptyForm()
  formError.value = ''
  formStep.value = 1
  formOpen.value = true
}

const openEdit = (vehicle) => {
  formMode.value = 'edit'
  formData.value = { ...buildEmptyForm(), ...vehicle }
  formError.value = ''
  formStep.value = 1
  formOpen.value = true
}

const closeForm = () => {
  formOpen.value = false
}

const nextFormStep = () => {
  if (formStep.value < formSteps.length && canGoNext.value) {
    formStep.value += 1
  } else if (!canGoNext.value) {
    formError.value = 'Plate, region, type, model, status, driver, and fuel type are required to continue.'
  }
}

const prevFormStep = () => {
  if (formStep.value > 1) {
    formStep.value -= 1
  }
}

const openIncident = () => {
  incidentMode.value = 'add'
  incidentForm.value = buildEmptyIncident()
  incidentError.value = ''
  incidentOpen.value = true
}

const openIncidentEdit = (incident) => {
  incidentMode.value = 'edit'
  incidentForm.value = { ...buildEmptyIncident(), ...incident }
  incidentError.value = ''
  incidentOpen.value = true
}

const openIncidentDetails = (incident) => {
  selectedIncident.value = incident
  incidentDetailsOpen.value = true
}

const saveIncident = () => {
  if (!incidentForm.value.vehicleId || !incidentForm.value.type || !incidentForm.value.date) {
    incidentError.value = 'Vehicle, date, and type are required.'
    return
  }
  if (incidentMode.value === 'add') {
    const newId = `INC-${Math.floor(1000 + Math.random() * 9000)}`
    incidents.value = [
      {
        ...incidentForm.value,
        id: newId
      },
      ...incidents.value
    ]
  } else {
    incidents.value = incidents.value.map((item) =>
      item.id === incidentForm.value.id ? { ...item, ...incidentForm.value } : item
    )
  }
  incidentOpen.value = false
}

const deleteIncident = (id) => {
  const incident = incidents.value.find((item) => item.id === id)
  if (!incident) return
  openConfirm({
    title: 'Delete Incident?',
    message: `This will permanently remove ${incident.id}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: () => {
      incidents.value = incidents.value.filter((item) => item.id !== id)
    }
  })
}


const saveForm = () => {
  if (
    !formData.value.plate ||
    !formData.value.region ||
    !formData.value.type ||
    !formData.value.model ||
    !formData.value.status ||
    !formData.value.driver ||
    !formData.value.fuelType
  ) {
    formError.value = 'Plate, region, type, model, status, driver, and fuel type are required.'
    formStep.value = 1
    return
  }

  if (formMode.value === 'add') {
    const newId = `VH-${Math.floor(1000 + Math.random() * 9000)}`
    vehicles.value = [
      {
        ...formData.value,
        id: newId,
        image:
          formData.value.image ||
          'https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80',
        driverImage:
          formData.value.driverImage ||
          'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=800&q=80'
      },
      ...vehicles.value
    ]
  } else {
    vehicles.value = vehicles.value.map((item) =>
      item.id === formData.value.id ? { ...item, ...formData.value } : item
    )
  }

  formOpen.value = false
}

const openConfirm = ({ title, message, confirmText, tone, action }) => {
  confirmTitle.value = title
  confirmMessage.value = message
  confirmButton.value = confirmText
  confirmTone.value = tone
  pendingAction.value = action
  confirmOpen.value = true
}

const runConfirm = () => {
  pendingAction.value()
  confirmOpen.value = false
}

const toggleStatus = (id) => {
  const vehicle = vehicles.value.find((item) => item.id === id)
  if (!vehicle) return
  const nextStatus = vehicle.status === 'Active' ? 'Inactive' : 'Active'
  openConfirm({
    title: `${nextStatus} Vehicle?`,
    message: `This will mark ${vehicle.id} as ${nextStatus.toLowerCase()}.`,
    confirmText: nextStatus,
    tone: 'warning',
    action: () => {
      vehicles.value = vehicles.value.map((item) =>
        item.id === id ? { ...item, status: nextStatus } : item
      )
    }
  })
}

const deleteVehicle = (id) => {
  const vehicle = vehicles.value.find((item) => item.id === id)
  if (!vehicle) return
  openConfirm({
    title: 'Delete Vehicle?',
    message: `This will permanently remove ${vehicle.id}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: () => {
      vehicles.value = vehicles.value.filter((item) => item.id !== id)
    }
  })
}
</script>

<style scoped>
.vehicle-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px;
}

.stat-card {
  padding: 18px;
  border-radius: 16px;
  border: 1px solid var(--fleet-border);
  background: #fff;
}

.stat-card p {
  margin: 0;
  font-size: 13px;
  color: var(--fleet-muted);
}

.stat-card h3 {
  margin: 8px 0 4px;
  font-size: 24px;
}

.stat-foot {
  font-size: 12px;
}

.text-success {
  color: var(--fleet-success);
}

.text-warning {
  color: var(--fleet-warning);
}

.text-danger {
  color: var(--fleet-danger);
}

.toolbar {
  padding: 18px;
}

.toolbar-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
}

.toolbar-search,
.toolbar-filter {
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 12px;
  background: #fff;
  min-width: 220px;
}

.toolbar-filter {
  cursor: pointer;
}

.toolbar-search input,
.toolbar-filter select {
  border: none;
  outline: none;
  background: transparent;
  font-size: 14px;
}

.toolbar-filter select {
  cursor: pointer;
  width: 100%;
}

.toolbar-search {
  flex: 1;
  min-width: 320px;
}

.toolbar-search input {
  width: 100%;
}

.toolbar-filter select {
  appearance: none;
}

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.clear-button {
  border: none;
  background: transparent;
  color: #94a3b8;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
}

.clear-button:hover {
  color: #475569;
}

.primary-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: none;
  border-radius: 12px;
  padding: 10px 16px;
  background: var(--fleet-primary);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}

.primary-button:hover {
  background: var(--fleet-primary-dark);
}

.ghost-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 16px;
  background: #fff;
  color: var(--fleet-text);
  font-weight: 600;
  cursor: pointer;
}

.ghost-button:hover {
  background: #f8fafc;
}

.table-wrap {
  overflow-x: auto;
}

.table-card {
  overflow: hidden;
}

.table-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  padding: 12px 16px 16px;
  border-top: 1px solid var(--fleet-border);
  flex-wrap: wrap;
}

.pager-actions {
  display: inline-flex;
  gap: 8px;
}

.pager-button {
  border: 1px solid var(--fleet-border);
  background: #fff;
  color: var(--fleet-text);
  font-size: 12px;
  font-weight: 600;
  padding: 6px 12px;
  border-radius: 10px;
  cursor: pointer;
}

.pager-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.align-right {
  text-align: right;
}

.driver-cell {
  display: inline-flex;
  align-items: center;
  gap: 10px;
}

.vehicle-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.vehicle-image {
  width: 54px;
  height: 36px;
  border-radius: 10px;
  object-fit: cover;
  border: 1px solid var(--fleet-border);
}

.vehicle-sub {
  font-size: 12px;
  margin-top: 2px;
}

.driver-photo {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  object-fit: cover;
  border: 1px solid var(--fleet-border);
  display: block;
}

.thumb-button {
  border: none;
  background: transparent;
  padding: 0;
  border-radius: 12px;
  cursor: pointer;
}

.thumb-button:focus-visible {
  outline: 2px solid rgba(37, 99, 235, 0.35);
  outline-offset: 2px;
}

.icon-button {
  border: none;
  background: transparent;
  width: 34px;
  height: 34px;
  border-radius: 10px;
  cursor: pointer;
  color: #2563eb;
}

.icon-button:hover {
  background: #eff6ff;
}

.icon-button.danger {
  color: #dc2626;
}

.icon-button.danger:hover {
  background: #fee2e2;
}

.icon-button.warn {
  color: #ea580c;
}

.icon-button.warn:hover {
  background: #ffedd5;
}

.icon-button.good {
  color: #16a34a;
}

.icon-button.good:hover {
  background: #dcfce7;
}

.empty-state {
  text-align: center;
  padding: 32px;
  color: var(--fleet-muted);
}

.section-card {
  padding: 18px 20px 22px;
}

.section-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 14px;
  border-bottom: 1px solid var(--fleet-border);
  margin-bottom: 14px;
}

.section-title {
  font-size: 16px;
  font-weight: 700;
}

.section-subtitle {
  font-size: 12px;
  margin-top: 4px;
}

.section-actions {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.details-card {
  padding: 20px 22px 24px;
}

.details-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--fleet-border);
}

.details-title {
  font-size: 18px;
  font-weight: 700;
}

.details-subtitle {
  font-size: 13px;
  margin-top: 4px;
}

.details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  padding-top: 18px;
}

.details-section {
  border: 1px solid var(--fleet-border);
  border-radius: 14px;
  padding: 14px;
  background: #fff;
}

.details-section h4 {
  margin: 0 0 10px;
  font-size: 14px;
}

.details-row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  font-size: 13px;
  padding: 6px 0;
  border-bottom: 1px dashed #e2e8f0;
}

.details-row:last-child {
  border-bottom: none;
}

.image-modal {
  padding: 16px 18px 20px;
}

.image-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--fleet-border);
  margin-bottom: 12px;
}

.image-title {
  font-weight: 600;
}

.full-image {
  width: 100%;
  height: 420px;
  border-radius: 14px;
  border: 1px solid var(--fleet-border);
  display: block;
  object-fit: cover;
}

@media (max-width: 720px) {
  .full-image {
    height: 300px;
  }
}

.form-card {
  padding: 18px 20px 22px;
  max-height: 80vh;
  overflow-y: auto;
  overflow-x: hidden;
}

.form-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--fleet-border);
}

.form-title {
  font-weight: 700;
  font-size: 18px;
}

.form-error {
  margin-top: 12px;
  padding: 10px 12px;
  border-radius: 10px;
  background: #fee2e2;
  color: #b91c1c;
  font-size: 13px;
}

.form-steps {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
  gap: 12px;
  margin-top: 14px;
}

.form-step {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border-radius: 12px;
  border: 1px solid var(--fleet-border);
  background: #fff;
}

.form-step.active {
  border-color: rgba(37, 99, 235, 0.45);
  background: #eff6ff;
}

.form-step.done {
  border-color: #bbf7d0;
  background: #f0fdf4;
}

.step-index {
  width: 28px;
  height: 28px;
  border-radius: 10px;
  display: grid;
  place-items: center;
  font-weight: 700;
  background: #e2e8f0;
  color: #334155;
}

.form-step.active .step-index {
  background: #1d4ed8;
  color: #fff;
}

.form-step.done .step-index {
  background: #16a34a;
  color: #fff;
}

.step-title {
  font-weight: 700;
  font-size: 13px;
}

.step-subtitle {
  font-size: 12px;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 12px 16px;
  margin-top: 16px;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 13px;
}

.form-field label {
  color: var(--fleet-muted);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.required {
  color: #dc2626;
  font-weight: 700;
}

.form-field input,
.form-field select {
  border: 1px solid var(--fleet-border);
  border-radius: 10px;
  padding: 9px 12px;
  font-size: 14px;
  background: #fff;
}

.form-field input:focus,
.form-field select:focus {
  outline: 2px solid rgba(37, 99, 235, 0.18);
  border-color: rgba(37, 99, 235, 0.6);
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 18px;
}

.hint {
  color: #94a3b8;
  font-size: 11px;
  font-weight: 500;
}

.tooltip {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: help;
}

.icon-tooltip {
  width: 22px;
  height: 22px;
  border-radius: 999px;
  color: #64748b;
}

.tooltip:focus-visible {
  outline: 2px solid rgba(37, 99, 235, 0.35);
  outline-offset: 2px;
}

.tooltip-text {
  position: absolute;
  bottom: calc(100% + 8px);
  left: 0;
  transform: translate(0, 6px);
  background: #0f172a;
  color: #fff;
  padding: 6px 8px;
  border-radius: 8px;
  font-size: 12px;
  white-space: nowrap;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.15s ease, transform 0.15s ease;
  box-shadow: 0 8px 16px rgba(15, 23, 42, 0.2);
  z-index: 2;
}

.form-card .tooltip-text {
  left: auto;
  right: 0;
  max-width: 200px;
  white-space: normal;
  text-align: left;
}

.tooltip:hover .tooltip-text,
.tooltip:focus-visible .tooltip-text {
  opacity: 1;
  transform: translate(0, 0);
}

.inline-actions .tooltip-text {
  left: auto;
  right: 0;
  transform: translateY(6px);
}

.inline-actions .tooltip:hover .tooltip-text,
.inline-actions .tooltip:focus-visible .tooltip-text {
  transform: translateY(0);
}

.thumb-button .tooltip-text {
  left: 0;
  right: auto;
}

.vehicle-table :deep(.v-table__wrapper) {
  background: #fff;
}

.vehicle-table :deep(table) {
  border-collapse: separate;
  border-spacing: 0;
}

.vehicle-table :deep(thead th) {
  background: #f8fafc;
  color: #475569;
  font-size: 13px;
  letter-spacing: 0.02em;
  text-transform: uppercase;
  font-weight: 700;
  padding: 14px 16px;
}

.vehicle-table :deep(tbody td) {
  padding: 14px 16px;
  background: #fff;
}

.vehicle-table :deep(tbody tr) {
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.06);
}

.vehicle-table :deep(tbody tr td) {
  border-bottom: 10px solid transparent;
}

.vehicle-table :deep(tbody tr:last-child td) {
  border-bottom: 0;
}

.vehicle-table :deep(tbody tr:nth-child(even) td) {
  background: #f8fafc;
}

.vehicle-table :deep(tbody tr td:first-child) {
  border-radius: 12px 0 0 12px;
}

.vehicle-table :deep(tbody tr td:last-child) {
  border-radius: 0 12px 12px 0;
}

.vehicle-table :deep(thead th:first-child) {
  border-radius: 12px 0 0 12px;
}

.vehicle-table :deep(thead th:last-child) {
  border-radius: 0 12px 12px 0;
}

.vehicle-table :deep(thead th:nth-child(1)),
.vehicle-table :deep(tbody td:nth-child(1)) {
  width: 260px;
}

.vehicle-table :deep(thead th:nth-child(2)),
.vehicle-table :deep(tbody td:nth-child(2)) {
  width: 160px;
}

.vehicle-table :deep(thead th:nth-child(3)),
.vehicle-table :deep(tbody td:nth-child(3)) {
  width: 140px;
}

.vehicle-table :deep(thead th:nth-child(4)),
.vehicle-table :deep(tbody td:nth-child(4)) {
  width: 140px;
}

.vehicle-table :deep(thead th:nth-child(5)),
.vehicle-table :deep(tbody td:nth-child(5)) {
  width: 220px;
}

.vehicle-table :deep(thead th:nth-child(6)),
.vehicle-table :deep(tbody td:nth-child(6)) {
  width: 150px;
}

.vehicle-table :deep(thead th:nth-child(7)),
.vehicle-table :deep(tbody td:nth-child(7)) {
  width: 180px;
}

.vehicle-table :deep(thead th.align-right),
.vehicle-table :deep(tbody td.align-right) {
  text-align: right;
}

@media (max-width: 980px) {
  .page-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .page-header .primary-button {
    width: 100%;
    justify-content: center;
  }

  .stats-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .toolbar-row {
    flex-direction: column;
    align-items: stretch;
  }

  .toolbar-search,
  .toolbar-filter {
    width: 100%;
    min-width: 0;
  }

  .section-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .section-actions {
    width: 100%;
  }

  .section-actions .primary-button {
    width: 100%;
    justify-content: center;
  }
}

@media (max-width: 720px) {
  .toolbar-row {
    flex-direction: column;
    align-items: stretch;
  }

  .toolbar-search {
    width: 100%;
  }

  .toolbar-filter {
    width: 100%;
  }

  .toolbar-actions {
    width: 100%;
    flex-direction: column;
    align-items: stretch;
  }

  .primary-button {
    width: 100%;
    justify-content: center;
  }
}

@media (max-width: 720px) {
  .vehicle-page {
    gap: 18px;
  }

  .stats-grid {
    grid-template-columns: 1fr;
  }

  .toolbar {
    padding: 10px;
  }

  .toolbar-row {
    gap: 8px;
  }

  .toolbar-search,
  .toolbar-filter {
    padding: 8px 10px;
    border-radius: 12px;
    min-height: 38px;
  }

  .toolbar-search v-icon,
  .toolbar-filter v-icon {
    font-size: 18px;
    color: var(--fleet-muted);
  }

  .toolbar-search input,
  .toolbar-filter select {
    font-size: 12px;
  }

  .toolbar-filter select {
    width: 100%;
  }

  .table-wrap {
    overflow-x: auto;
  }

  .table-base {
    width: 100%;
    min-width: 980px;
  }

  .table-base th,
  .table-base td {
    padding: 10px 12px;
    font-size: 12px;
    white-space: nowrap;
  }

  .details-card,
  .form-card,
  .image-modal {
    padding: 16px;
  }

  .details-grid,
  .form-grid {
    grid-template-columns: 1fr;
  }

  .form-steps {
    grid-template-columns: 1fr;
  }

  .form-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .form-actions .ghost-button,
  .form-actions .primary-button {
    width: 100%;
    justify-content: center;
  }

  .full-image {
    height: 240px;
  }

  :deep(.v-overlay__content) {
    max-width: calc(100% - 24px) !important;
    margin: 12px;
  }
}
</style>
